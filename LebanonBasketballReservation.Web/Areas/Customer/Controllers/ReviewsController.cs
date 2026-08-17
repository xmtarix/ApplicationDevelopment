using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Reservation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

public class ReviewsController : CustomerBaseController
{
    private readonly IReservationService _reservations;

    public ReviewsController(
        IReservationService reservations,
        UserManager<ApplicationUser> userManager) : base(userManager)
        => _reservations = reservations;

    [HttpGet]
    public async Task<IActionResult> Create(int reservationId, CancellationToken cancellationToken)
    {
        var r = await _reservations.GetByIdAsync(reservationId, CustomerId, cancellationToken: cancellationToken);
        if (r is null) return NotFound();

        if (!r.CanReview)
        {
            Error(r.HasReview
                ? "You have already reviewed this reservation."
                : "You can review a booking once it has been completed.");

            return RedirectToAction("Details", "Reservations", new { id = reservationId });
        }

        return View(new CreateReviewViewModel { ReservationId = reservationId, Reservation = r });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            vm.Reservation = await _reservations.GetByIdAsync(vm.ReservationId, CustomerId, cancellationToken: cancellationToken);
            return View(vm);
        }

        try
        {
            await _reservations.SubmitReviewAsync(vm.ReservationId, CustomerId, vm.Rating, vm.Comment, cancellationToken);
            Success("Thanks for your review!");
            return RedirectToAction("Details", "Reservations", new { id = vm.ReservationId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Reservation = await _reservations.GetByIdAsync(vm.ReservationId, CustomerId, cancellationToken: cancellationToken);
            return View(vm);
        }
    }
}
