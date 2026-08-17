using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Court;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

public class TimeSlotsController : ManagerBaseController
{
    private readonly ICourtService _courts;
    private readonly IStadiumService _stadiums;
    private readonly IDateTimeProvider _clock;

    public TimeSlotsController(
        ICourtService courts,
        IStadiumService stadiums,
        IDateTimeProvider clock,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _courts = courts;
        _stadiums = stadiums;
        _clock = clock;
    }

    /// <summary>Loads the court after confirming the signed-in manager owns its stadium.</summary>
    private async Task<CourtDto?> GetOwnedCourtAsync(int courtId, CancellationToken cancellationToken)
    {
        var court = await _courts.GetByIdAsync(courtId, cancellationToken);
        if (court is null) return null;

        try
        {
            await _stadiums.GetOwnedByIdAsync(court.StadiumId, ManagerId, cancellationToken);
            return court;
        }
        catch (DomainException) { return null; }
    }

    [HttpGet]
    public async Task<IActionResult> Generate(int courtId, DateOnly? date, CancellationToken cancellationToken)
    {
        var court = await GetOwnedCourtAsync(courtId, cancellationToken);
        if (court is null) return NotFound();

        var selected = date ?? _clock.Today;

        return View(new TimeSlotManagerViewModel
        {
            Court = court,
            Date = selected,
            Slots = await _courts.GetTimeSlotsAsync(courtId, selected, cancellationToken),
            GenerateForm = new GenerateTimeSlotsDto
            {
                CourtId = courtId,
                FromDate = _clock.Today,
                ToDate = _clock.Today.AddDays(7)
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Generate(GenerateTimeSlotsDto dto, CancellationToken cancellationToken)
    {
        var court = await GetOwnedCourtAsync(dto.CourtId, cancellationToken);
        if (court is null) return NotFound();

        if (!ModelState.IsValid)
            return View(await BuildViewModelAsync(court, dto, cancellationToken));

        try
        {
            var created = await _courts.GenerateTimeSlotsAsync(dto, ManagerId, cancellationToken);

            if (created == 0)
                Error("No new slots were created — they already exist for that range.");
            else
                Success($"{created} time slot{(created == 1 ? "" : "s")} generated.");

            return RedirectToAction(nameof(Generate), new { courtId = dto.CourtId, date = dto.FromDate });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildViewModelAsync(court, dto, cancellationToken));
        }
    }

    private async Task<TimeSlotManagerViewModel> BuildViewModelAsync(CourtDto court, GenerateTimeSlotsDto dto, CancellationToken cancellationToken)
        => new()
        {
            Court = court,
            Date = dto.FromDate,
            Slots = await _courts.GetTimeSlotsAsync(court.Id, dto.FromDate, cancellationToken),
            GenerateForm = dto
        };

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int courtId, DateOnly? date, CancellationToken cancellationToken)
    {
        try
        {
            await _courts.DeleteTimeSlotAsync(id, ManagerId, cancellationToken);
            Success("Slot removed.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Generate), new { courtId, date });
    }

    [HttpPost]
    public async Task<IActionResult> ClearFuture(int courtId, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await _courts.ClearFutureSlotsAsync(courtId, ManagerId, cancellationToken);
            Success($"{removed} unbooked slot{(removed == 1 ? "" : "s")} removed.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Generate), new { courtId });
    }
}
