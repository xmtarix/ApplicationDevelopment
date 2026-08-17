using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Controllers;

/// <summary>
/// Read-only JSON endpoints backing the cascading Governorate → District → Area dropdowns.
/// Public because the same lists already appear on the anonymous stadium search page.
/// </summary>
[Route("[controller]/[action]")]
public class LocationsController : Controller
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations) => _locations = locations;

    [HttpGet]
    public async Task<IActionResult> Districts(int governorateId, CancellationToken cancellationToken)
    {
        var districts = await _locations.GetDistrictsByGovernorateAsync(governorateId, cancellationToken);
        return Json(districts.Select(d => new { id = d.Id, name = d.Name }));
    }

    [HttpGet]
    public async Task<IActionResult> Areas(int districtId, CancellationToken cancellationToken)
    {
        var areas = await _locations.GetAreasByDistrictAsync(districtId, cancellationToken);
        return Json(areas.Select(a => new { id = a.Id, name = a.Name }));
    }
}
