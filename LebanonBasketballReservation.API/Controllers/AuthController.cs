using LebanonBasketballReservation.API.Models;
using LebanonBasketballReservation.API.Services;
using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LebanonBasketballReservation.API.Controllers;

/// <summary>Issues the bearer tokens that the rest of the API requires.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokens,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Exchanges email and password for a JWT bearer token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { error = "This account has been deactivated." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized(new { error = "Account locked due to repeated failed attempts. Try again later." });

        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid email or password." });

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokens.CreateToken(user, roles);

        _logger.LogInformation("API token issued for {Email}", user.Email);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            Roles = roles.ToList()
        });
    }

    /// <summary>Registers a customer account and returns a token for it.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { error = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // The public API only creates customers; manager accounts go through the web app.
        await _userManager.AddToRoleAsync(user, RoleNames.Customer);
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokens.CreateToken(user, roles);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            Roles = roles.ToList()
        });
    }

    /// <summary>Returns the profile behind the supplied bearer token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            phoneNumber = user.PhoneNumber,
            roles = await _userManager.GetRolesAsync(user)
        });
    }
}
