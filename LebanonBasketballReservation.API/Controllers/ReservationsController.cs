using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LebanonBasketballReservation.API.Controllers;

/// <summary>Reservations belonging to the authenticated caller.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations) => _reservations = reservations;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole(RoleNames.Admin);

    /// <summary>Lists the caller's reservations, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
        => Ok(await _reservations.GetByCustomerAsync(UserId, cancellationToken));

    /// <summary>Fetches one reservation. Returns 404 when it is not the caller's.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var r = await _reservations.GetByIdAsync(id, UserId, IsAdmin, cancellationToken);
        return r is null ? NotFound(new { error = "Reservation not found." }) : Ok(r);
    }

    /// <summary>Books an available time slot.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
    {
        // Domain failures are translated by ApiExceptionMiddleware.
        var created = await _reservations.CreateAsync(dto, UserId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Cancels one of the caller's reservations.</summary>
    [HttpPut("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelReservationDto? dto, CancellationToken cancellationToken)
    {
        await _reservations.CancelAsync(id, UserId, dto?.Reason, cancellationToken);
        return Ok(new { message = "Reservation cancelled." });
    }

    /// <summary>Leaves a rating for a completed reservation.</summary>
    [HttpPost("{id:int}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Review(int id, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        await _reservations.SubmitReviewAsync(id, UserId, request.Rating, request.Comment, cancellationToken);
        return Ok(new { message = "Review submitted." });
    }
}

public class SubmitReviewRequest
{
    /// <summary>1 to 5 stars.</summary>
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
