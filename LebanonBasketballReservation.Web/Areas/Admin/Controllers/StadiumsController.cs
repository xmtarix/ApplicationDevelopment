using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

public class StadiumsController : AdminBaseController
{
    private readonly IStadiumService _stadiums;

    public StadiumsController(IStadiumService stadiums) => _stadiums = stadiums;

    public async Task<IActionResult> Index(StadiumStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Status"] = status;
        return View(await _stadiums.GetAllForAdminAsync(status, page, 20, cancellationToken));
    }

    public async Task<IActionResult> Pending(int page = 1, CancellationToken cancellationToken = default)
        => View(await _stadiums.GetAllForAdminAsync(StadiumStatus.Pending, page, 20, cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var stadium = await _stadiums.GetByIdAsync(id, cancellationToken);
        return stadium is null ? NotFound() : View(stadium);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id, string? returnAction, CancellationToken cancellationToken)
    {
        try
        {
            await _stadiums.ApproveAsync(id, cancellationToken);
            Success("Stadium approved and published.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(returnAction ?? nameof(Pending));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason, string? returnAction, CancellationToken cancellationToken)
    {
        try
        {
            await _stadiums.RejectAsync(id, reason, cancellationToken);
            Success("Stadium rejected and the manager has been notified.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(returnAction ?? nameof(Pending));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _stadiums.SetActiveAsync(id, isActive, cancellationToken);
            Success(isActive ? "Stadium is now visible to customers." : "Stadium hidden from customers.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }
}
