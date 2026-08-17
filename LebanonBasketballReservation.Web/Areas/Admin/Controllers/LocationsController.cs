using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Admin.Controllers;

public class LocationsController : AdminBaseController
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations) => _locations = locations;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _locations.GetHierarchyAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateGovernorate(string name, string? nameAr, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.CreateGovernorateAsync(name, nameAr ?? "", cancellationToken);
            Success($"Governorate \"{name}\" added.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateDistrict(string name, string? nameAr, int governorateId, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.CreateDistrictAsync(name, nameAr ?? "", governorateId, cancellationToken);
            Success($"District \"{name}\" added.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateArea(string name, string? nameAr, int districtId, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.CreateAreaAsync(name, nameAr ?? "", districtId, cancellationToken);
            Success($"Area \"{name}\" added.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGovernorate(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.DeleteGovernorateAsync(id, cancellationToken);
            Success("Governorate deleted.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDistrict(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.DeleteDistrictAsync(id, cancellationToken);
            Success("District deleted.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteArea(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _locations.DeleteAreaAsync(id, cancellationToken);
            Success("Area deleted.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }
}
