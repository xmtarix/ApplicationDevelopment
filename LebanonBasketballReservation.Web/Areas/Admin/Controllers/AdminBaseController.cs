using LebanonBasketballReservation.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
[Area("Admin")]
public abstract class AdminBaseController : Controller
{
    /// <summary>Shows a green banner on the next page.</summary>
    protected void Success(string message) => TempData["Success"] = message;

    /// <summary>Shows a red banner on the next page.</summary>
    protected void Error(string message) => TempData["Error"] = message;
}
