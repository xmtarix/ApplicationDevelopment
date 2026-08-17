using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

public class NotificationsController : CustomerBaseController
{
    private readonly INotificationService _notifications;

    public NotificationsController(
        INotificationService notifications,
        UserManager<ApplicationUser> userManager) : base(userManager)
        => _notifications = notifications;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _notifications.GetByUserAsync(CustomerId, 50, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(id, CustomerId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notifications.MarkAllReadAsync(CustomerId, cancellationToken);
        Success("All notifications marked as read.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _notifications.DeleteAsync(id, CustomerId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Feeds the navbar bell without a full page load.</summary>
    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
        => Json(new { count = await _notifications.GetUnreadCountAsync(CustomerId, cancellationToken) });
}
