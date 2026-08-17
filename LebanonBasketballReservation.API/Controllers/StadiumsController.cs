using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.API.Controllers;

/// <summary>Public catalogue of stadiums, courts and availability.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StadiumsController : ControllerBase
{
    private readonly IStadiumService _stadiums;
    private readonly IAvailabilityService _availability;
    private readonly ICourtService _courts;

    public StadiumsController(IStadiumService stadiums, IAvailabilityService availability, ICourtService courts)
    {
        _stadiums = stadiums;
        _availability = availability;
        _courts = courts;
    }

    /// <summary>Searches active stadiums with paging and filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StadiumDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] StadiumSearchDto filter, CancellationToken cancellationToken)
        => Ok(await _stadiums.SearchAsync(filter, cancellationToken));

    /// <summary>Full details for one stadium, including its courts and reviews.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StadiumDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var s = await _stadiums.GetByIdAsync(id, cancellationToken);
        return s is null ? NotFound(new { error = "Stadium not found." }) : Ok(s);
    }

    /// <summary>Courts belonging to a stadium.</summary>
    [HttpGet("{id:int}/courts")]
    [ProducesResponseType(typeof(IEnumerable<CourtDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Courts(int id, CancellationToken cancellationToken)
        => Ok(await _courts.GetByStadiumAsync(id, cancellationToken));

    /// <summary>Bookable slots for a court on a given date (yyyy-MM-dd).</summary>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(IEnumerable<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Availability(int id, [FromQuery] int courtId, [FromQuery] string? date, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest(new { error = "Provide a valid date in yyyy-MM-dd format." });

        return Ok(await _availability.GetAvailableSlotsAsync(courtId, parsed, cancellationToken));
    }

    /// <summary>Two weeks of availability for a court, grouped by day.</summary>
    [HttpGet("{id:int}/calendar")]
    [ProducesResponseType(typeof(IEnumerable<DayAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calendar(int id, [FromQuery] int courtId, [FromQuery] int days = 14, CancellationToken cancellationToken = default)
        => Ok(await _availability.GetAvailabilityCalendarAsync(courtId, DateOnly.FromDateTime(DateTime.Today), days, cancellationToken));

    /// <summary>Customer reviews for a stadium.</summary>
    [HttpGet("{id:int}/reviews")]
    [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reviews(int id, CancellationToken cancellationToken)
        => Ok(await _stadiums.GetReviewsAsync(id, cancellationToken));
}
