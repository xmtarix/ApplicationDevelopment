using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

public class DashboardController : ManagerBaseController
{
    private readonly IReportService _reports;
    private readonly IReservationService _reservations;
    private readonly IStadiumService _stadiums;

    public DashboardController(
        IReportService reports,
        IReservationService reservations,
        IStadiumService stadiums,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _reports = reports;
        _reservations = reservations;
        _stadiums = stadiums;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stats = await _reports.GetManagerDashboardStatsAsync(ManagerId, cancellationToken);
        var reservations = (await _reservations.GetByManagerAsync(ManagerId, cancellationToken)).ToList();

        return View(new ManagerDashboardViewModel
        {
            Stats = stats,
            MyStadiums = await _stadiums.GetByManagerAsync(ManagerId, cancellationToken),
            // Requests awaiting a decision come first — that is the manager's actual to-do list.
            PendingRequests = reservations
                .Where(r => r.Status == Data.Enums.ReservationStatus.Pending)
                .OrderBy(r => r.StartsAt)
                .Take(5)
                .ToList(),
            RecentReservations = reservations.Take(10).ToList()
        });
    }
}
