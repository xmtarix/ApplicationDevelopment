using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Web.ViewModels.Reservation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

public class ReservationsController : CustomerBaseController
{
    private readonly IReservationService _reservations;
    private readonly IStadiumService _stadiums;
    private readonly ICourtService _courts;
    private readonly IAvailabilityService _availability;
    private readonly IDateTimeProvider _clock;

    public ReservationsController(
        IReservationService reservations,
        IStadiumService stadiums,
        ICourtService courts,
        IAvailabilityService availability,
        IDateTimeProvider clock,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _reservations = reservations;
        _stadiums = stadiums;
        _courts = courts;
        _availability = availability;
        _clock = clock;
    }

    public async Task<IActionResult> Index(ReservationStatus? status, CancellationToken cancellationToken)
    {
        var all = (await _reservations.GetByCustomerAsync(CustomerId, cancellationToken)).ToList();
        var today = _clock.Today;

        ViewData["Status"] = status;

        return View(new ReservationListViewModel
        {
            Upcoming = all.Where(r => r.Date >= today && r.Status is ReservationStatus.Pending or ReservationStatus.Confirmed).OrderBy(r => r.StartsAt).ToList(),
            Past = all.Where(r => r.Date < today || r.Status is ReservationStatus.Completed or ReservationStatus.Cancelled or ReservationStatus.Rejected).ToList(),
            StatusFilter = status
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        // Returns null unless this reservation belongs to the signed-in customer.
        var r = await _reservations.GetByIdAsync(id, CustomerId, cancellationToken: cancellationToken);
        return r is null ? NotFound() : View(r);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int courtId, int? timeSlotId, DateOnly? date, CancellationToken cancellationToken)
    {
        var court = await _courts.GetByIdAsync(courtId, cancellationToken);
        if (court is null) return NotFound();

        var stadium = await _stadiums.GetByIdAsync(court.StadiumId, cancellationToken);
        if (stadium is null || stadium.Status != StadiumStatus.Active)
        {
            Error("This stadium is not currently accepting reservations.");
            return RedirectToAction("Index", "Stadiums", new { area = "" });
        }

        var vm = new CreateReservationViewModel
        {
            CourtId = courtId,
            StadiumId = court.StadiumId,
            Court = court,
            Stadium = stadium,
            Calendar = await _availability.GetAvailabilityCalendarAsync(courtId, _clock.Today, 14, cancellationToken)
        };

        // Preselect the slot the customer clicked, so the review step is one step, not two.
        if (timeSlotId.HasValue)
        {
            var slot = await _availability.GetSlotByIdAsync(timeSlotId.Value, cancellationToken);
            if (slot is not null && slot.CourtId == courtId)
            {
                vm.TimeSlotId = slot.Id;
                vm.SelectedSlot = slot;
                vm.Date = slot.Date;
            }
        }
        else if (date.HasValue)
        {
            vm.Date = date.Value;
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReservationViewModel vm, CancellationToken cancellationToken)
    {
        if (vm.TimeSlotId <= 0)
            ModelState.AddModelError(nameof(vm.TimeSlotId), "Please choose a time slot.");

        if (!ModelState.IsValid)
            return await RedisplayCreateAsync(vm, cancellationToken);

        try
        {
            var created = await _reservations.CreateAsync(
                new CreateReservationDto { TimeSlotId = vm.TimeSlotId, Notes = vm.Notes },
                CustomerId,
                cancellationToken);

            Success("Reservation submitted! You'll be notified once the manager confirms it.");
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (DomainException ex)
        {
            // Re-render with the message in context rather than bouncing to a blank form.
            ModelState.AddModelError(string.Empty, ex.Message);
            return await RedisplayCreateAsync(vm, cancellationToken);
        }
    }

    private async Task<IActionResult> RedisplayCreateAsync(CreateReservationViewModel vm, CancellationToken cancellationToken)
    {
        vm.Court = await _courts.GetByIdAsync(vm.CourtId, cancellationToken);
        vm.Stadium = vm.Court is null ? null : await _stadiums.GetByIdAsync(vm.Court.StadiumId, cancellationToken);
        vm.Calendar = await _availability.GetAvailabilityCalendarAsync(vm.CourtId, _clock.Today, 14, cancellationToken);
        vm.SelectedSlot = null;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var r = await _reservations.GetByIdAsync(id, CustomerId, cancellationToken: cancellationToken);
        if (r is null) return NotFound();

        if (!r.CanCancel)
        {
            Error("This reservation can no longer be cancelled.");
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new CancelReservationViewModel { Id = id, Reservation = r });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(CancelReservationViewModel vm, CancellationToken cancellationToken)
    {
        try
        {
            await _reservations.CancelAsync(vm.Id, CustomerId, vm.Reason, cancellationToken);
            Success("Reservation cancelled.");
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            Error(ex.Message);
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }
    }
}
