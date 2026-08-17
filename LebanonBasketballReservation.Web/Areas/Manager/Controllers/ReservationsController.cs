using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

public class ReservationsController : ManagerBaseController
{
    private readonly IReservationService _reservations;

    public ReservationsController(
        IReservationService reservations,
        UserManager<ApplicationUser> userManager) : base(userManager)
        => _reservations = reservations;

    public async Task<IActionResult> Index(ReservationStatus? status, CancellationToken cancellationToken)
    {
        var all = (await _reservations.GetByManagerAsync(ManagerId, cancellationToken)).ToList();

        ViewData["Status"] = status;
        ViewData["PendingCount"] = all.Count(r => r.Status == ReservationStatus.Pending);

        var filtered = status.HasValue
            ? all.Where(r => r.Status == status.Value).ToList()
            : all;

        return View(filtered);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        // The service returns null unless this manager owns the reservation's stadium.
        var reservation = await _reservations.GetByIdAsync(id, ManagerId, cancellationToken: cancellationToken);
        return reservation is null ? NotFound() : View(reservation);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _reservations.ConfirmAsync(id, ManagerId, cancellationToken);
            Success("Reservation confirmed and the customer has been notified.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await _reservations.RejectAsync(id, ManagerId, reason, cancellationToken);
            Success("Reservation rejected and the slot is available again.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }
}
