using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

[Authorize(Roles = RoleNames.StadiumManager)]
[Area("Manager")]
public abstract class ManagerBaseController : Controller
{
    protected readonly UserManager<ApplicationUser> UserManager;

    protected ManagerBaseController(UserManager<ApplicationUser> userManager) => UserManager = userManager;

    /// <summary>The signed-in manager's id. Never null inside an [Authorize]d action.</summary>
    protected string ManagerId => UserManager.GetUserId(User)!;

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;
}
