using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signIn,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signIn = signIn;
        _logger = logger;
    }

    /// <summary>Sends each role to the dashboard that is actually useful to it.</summary>
    private async Task<IActionResult> RedirectToRoleHomeAsync(ApplicationUser user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        if (await _userManager.IsInRoleAsync(user, RoleNames.StadiumManager))
            return RedirectToAction("Index", "Dashboard", new { area = "Manager" });

        if (await _userManager.IsInRoleAsync(user, RoleNames.Customer))
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Deactivated accounts must not be able to sign in. Checking before the password
        // attempt keeps a disabled account from consuming lockout attempts either way.
        if (user is not null && !user.IsActive)
        {
            _logger.LogWarning("Blocked sign-in for deactivated account {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in", model.Email);
            return await RedirectToRoleHomeAsync(user!, returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again in 15 minutes.");
            return View(model);
        }

        // Deliberately vague, so this cannot be used to discover which emails exist.
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var role = model.IsManager ? RoleNames.StadiumManager : RoleNames.Customer;
            await _userManager.AddToRoleAsync(user, role);
            await _signIn.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("New {Role} registered: {Email}", role, email);
            TempData["Success"] = $"Welcome, {user.FirstName}! Your account is ready.";

            return await RedirectToRoleHomeAsync(user, returnUrl: null);
        }

        foreach (var e in result.Errors)
            ModelState.AddModelError(string.Empty, e.Description);

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        return View(new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email,
            MemberSince = user.CreatedAt
        });
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        // Repopulate read-only fields so a validation failure still renders a complete page.
        model.Email = user.Email;
        model.MemberSince = user.CreatedAt;

        if (!ModelState.IsValid) return View(model);

        var wantsPasswordChange = !string.IsNullOrWhiteSpace(model.NewPassword);

        if (wantsPasswordChange && string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password to set a new one.");
            return View(model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.DateOfBirth = model.DateOfBirth;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var e in updateResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        if (wantsPasswordChange)
        {
            var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (!passwordResult.Succeeded)
            {
                foreach (var e in passwordResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Keep the current browser signed in after the credential change.
            await _signIn.RefreshSignInAsync(user);
            TempData["Success"] = "Profile and password updated.";
        }
        else
        {
            TempData["Success"] = "Profile updated.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
