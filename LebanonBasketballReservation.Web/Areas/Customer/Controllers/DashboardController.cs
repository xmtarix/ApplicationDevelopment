using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

public class DashboardController : CustomerBaseController
{
    private readonly IReservationService _reservations;
    private readonly INotificationService _notifications;
    private readonly IFavoriteService _favorites;
    private readonly IDateTimeProvider _clock;

    public DashboardController(
        IReservationService reservations,
        INotificationService notifications,
        IFavoriteService favorites,
        IDateTimeProvider clock,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _reservations = reservations;
        _notifications = notifications;
        _favorites = favorites;
        _clock = clock;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var all = (await _reservations.GetByCustomerAsync(CustomerId, cancellationToken)).ToList();
        var today = _clock.Today;

        return View(new CustomerDashboardViewModel
        {
            UpcomingReservations = all
                .Where(r => r.Date >= today && r.Status is ReservationStatus.Pending or ReservationStatus.Confirmed)
                .OrderBy(r => r.StartsAt)
                .Take(5)
                .ToList(),

            PastReservations = all
                .Where(r => r.Status == ReservationStatus.Completed)
                .Take(5)
                .ToList(),

            // Surfacing these turns a completed booking into a review without the customer hunting for it.
            AwaitingReview = all.Where(r => r.CanReview).Take(3).ToList(),

            Favorites = (await _favorites.GetFavoritesAsync(CustomerId, cancellationToken)).Take(3).ToList(),

            TotalReservations = all.Count,
            CompletedReservations = all.Count(r => r.Status == ReservationStatus.Completed),
            TotalSpent = all
                .Where(r => r.Status is ReservationStatus.Confirmed or ReservationStatus.Completed)
                .Sum(r => r.TotalPrice),
            UnreadNotifications = await _notifications.GetUnreadCountAsync(CustomerId, cancellationToken)
        });
    }
}
