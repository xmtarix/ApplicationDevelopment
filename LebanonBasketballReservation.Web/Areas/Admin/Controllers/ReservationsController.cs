using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

public class ReservationsController : AdminBaseController
{
    private readonly IReservationService _reservations;
    private readonly IReportService _reports;
    private readonly IStadiumService _stadiums;

    public ReservationsController(IReservationService reservations, IReportService reports, IStadiumService stadiums)
    {
        _reservations = reservations;
        _reports = reports;
        _stadiums = stadiums;
    }

    public async Task<IActionResult> Index(ReservationFilterDto filter, CancellationToken cancellationToken)
    {
        ViewData["Filter"] = filter;
        ViewData["Stadiums"] = (await _stadiums.GetAllForAdminAsync(null, 1, 100, cancellationToken)).Items;
        return View(await _reservations.GetAllForAdminAsync(filter, cancellationToken));
    }

    public async Task<IActionResult> Report(DateTime? from, DateTime? to, int? stadiumId, CancellationToken cancellationToken)
    {
        // Default to the last 30 days when the page is opened without a range.
        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow.Date;

        if (toDate < fromDate) (fromDate, toDate) = (toDate, fromDate);

        ViewData["Stadiums"] = (await _stadiums.GetAllForAdminAsync(null, 1, 100, cancellationToken)).Items;
        ViewData["SelectedStadiumId"] = stadiumId;

        return View(await _reports.GetReservationReportAsync(fromDate, toDate, stadiumId, cancellationToken));
    }
}
