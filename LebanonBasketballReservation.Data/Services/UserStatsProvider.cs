using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Services;

/// <summary>
/// Counts users per role with a single grouped query over the Identity tables, rather than
/// loading every user and calling GetRolesAsync one at a time.
/// </summary>
public class UserStatsProvider : IUserStatsProvider
{
    private readonly ApplicationDbContext _context;

    public UserStatsProvider(ApplicationDbContext context) => _context = context;

    public async Task<UserCounts> GetUserCountsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _context.Users.CountAsync(cancellationToken);

        var perRole = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            group userRole by role.Name into g
            select new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountFor(string role) => perRole.FirstOrDefault(r => r.Role == role)?.Count ?? 0;

        return new UserCounts(
            Total: total,
            Customers: CountFor(RoleNames.Customer),
            Managers: CountFor(RoleNames.StadiumManager),
            Admins: CountFor(RoleNames.Admin));
    }
}
