using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Court;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

public class CourtsController : ManagerBaseController
{
    private readonly ICourtService _courts;
    private readonly IStadiumService _stadiums;

    public CourtsController(
        ICourtService courts,
        IStadiumService stadiums,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _courts = courts;
        _stadiums = stadiums;
    }

    public async Task<IActionResult> Index(int stadiumId, CancellationToken cancellationToken)
    {
        try
        {
            var stadium = await _stadiums.GetOwnedByIdAsync(stadiumId, ManagerId, cancellationToken);

            return View(new CourtListViewModel
            {
                Stadium = stadium,
                Courts = await _courts.GetByStadiumAsync(stadiumId, cancellationToken)
            });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
    }

    [HttpGet]
    public async Task<IActionResult> Create(int stadiumId, CancellationToken cancellationToken)
    {
        try
        {
            var stadium = await _stadiums.GetOwnedByIdAsync(stadiumId, ManagerId, cancellationToken);
            ViewData["StadiumName"] = stadium.Name;
            return View(new CreateCourtDto { StadiumId = stadiumId });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCourtDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return await RedisplayCreateAsync(dto, cancellationToken);

        try
        {
            await _courts.CreateAsync(dto, ManagerId, cancellationToken);
            Success($"Court \"{dto.Name}\" added.");
            return RedirectToAction(nameof(Index), new { stadiumId = dto.StadiumId });
        }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await RedisplayCreateAsync(dto, cancellationToken);
        }
    }

    private async Task<IActionResult> RedisplayCreateAsync(CreateCourtDto dto, CancellationToken cancellationToken)
    {
        var stadium = await _stadiums.GetByIdAsync(dto.StadiumId, cancellationToken);
        ViewData["StadiumName"] = stadium?.Name ?? "";
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var court = await _courts.GetByIdAsync(id, cancellationToken);
        if (court is null) return NotFound();

        // Confirm ownership before revealing anything about the court.
        try { await _stadiums.GetOwnedByIdAsync(court.StadiumId, ManagerId, cancellationToken); }
        catch (ForbiddenException) { return Forbid(); }
        catch (NotFoundException) { return NotFound(); }

        ViewData["StadiumName"] = court.StadiumName;

        return View(new UpdateCourtDto
        {
            Id = court.Id,
            Name = court.Name,
            Description = court.Description,
            IsIndoor = court.IsIndoor,
            Capacity = court.Capacity,
            HourlyPrice = court.HourlyPrice,
            StadiumId = court.StadiumId,
            Status = court.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateCourtDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(dto);

        try
        {
            await _courts.UpdateAsync(dto, ManagerId, cancellationToken);
            Success("Court updated.");
            return RedirectToAction(nameof(Index), new { stadiumId = dto.StadiumId });
        }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int stadiumId, CancellationToken cancellationToken)
    {
        try
        {
            await _courts.DeleteAsync(id, ManagerId, cancellationToken);
            Success("Court deleted.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index), new { stadiumId });
    }
}
