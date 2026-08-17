using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

[Authorize(Roles = RoleNames.Customer)]
[Area("Customer")]
public abstract class CustomerBaseController : Controller
{
    protected readonly UserManager<ApplicationUser> UserManager;

    protected CustomerBaseController(UserManager<ApplicationUser> userManager) => UserManager = userManager;

    /// <summary>The signed-in customer's id. Never null inside an [Authorize]d action.</summary>
    protected string CustomerId => UserManager.GetUserId(User)!;

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;
}
