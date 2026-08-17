using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IReportService _reports;

    public DashboardController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _reports.GetAdminDashboardStatsAsync(cancellationToken));
}
