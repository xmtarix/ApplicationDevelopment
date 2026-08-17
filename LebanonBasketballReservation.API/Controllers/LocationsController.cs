using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.API.Controllers;

/// <summary>The Lebanese location hierarchy used to filter stadiums.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations) => _locations = locations;

    /// <summary>All governorates.</summary>
    [HttpGet("governorates")]
    [ProducesResponseType(typeof(IEnumerable<GovernorateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Governorates(CancellationToken cancellationToken)
        => Ok(await _locations.GetGovernoratesAsync(cancellationToken));

    /// <summary>Districts within a governorate.</summary>
    [HttpGet("districts/{governorateId:int}")]
    [ProducesResponseType(typeof(IEnumerable<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Districts(int governorateId, CancellationToken cancellationToken)
        => Ok(await _locations.GetDistrictsByGovernorateAsync(governorateId, cancellationToken));

    /// <summary>Areas within a district.</summary>
    [HttpGet("areas/{districtId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Areas(int districtId, CancellationToken cancellationToken)
        => Ok(await _locations.GetAreasByDistrictAsync(districtId, cancellationToken));

    /// <summary>The full governorate → district → area tree in one call.</summary>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(IEnumerable<GovernorateTreeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Hierarchy(CancellationToken cancellationToken)
        => Ok(await _locations.GetHierarchyAsync(cancellationToken));
}
