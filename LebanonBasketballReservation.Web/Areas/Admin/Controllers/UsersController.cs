using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? role, int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                u.Email!.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Roles for the page in one round-trip rather than a query per user.
        var rolesByUser = await GetRolesForAsync(users.Select(u => u.Id).ToList(), cancellationToken);

        var rows = users
            .Select(u => new UserRowViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles = rolesByUser.GetValueOrDefault(u.Id, [])
            })
            .Where(r => string.IsNullOrWhiteSpace(role) || r.Roles.Contains(role))
            .ToList();

        return View(new UserListViewModel
        {
            Users = rows,
            Search = search,
            Role = role,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    private async Task<Dictionary<string, List<string>>> GetRolesForAsync(List<string> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (
            from userRole in _context.UserRoles
            join r in _context.Roles on userRole.RoleId equals r.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.RoleName).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // An admin locking themselves out would leave no way back in.
        if (user.Id == _userManager.GetUserId(User))
        {
            Error("You cannot deactivate your own account.");
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            Error(string.Join("; ", result.Errors.Select(e => e.Description)));
            return RedirectToAction(nameof(Index));
        }

        // Rotating the security stamp invalidates existing cookies, so a deactivated user
        // is signed out on their next request instead of lingering until the cookie expires.
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("User {UserId} {Action} by admin", user.Id, user.IsActive ? "activated" : "deactivated");
        Success($"{user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(string id, string role, CancellationToken cancellationToken)
    {
        if (!RoleNames.All.Contains(role))
        {
            Error("Unknown role.");
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            Error("You cannot change your own role.");
            return RedirectToAction(nameof(Index));
        }

        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0) await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("User {UserId} role changed to {Role}", user.Id, role);
        Success($"{user.FullName} is now a {role}.");

        return RedirectToAction(nameof(Index));
    }
}
