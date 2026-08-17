using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(CreateReservationDto dto, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the reservation only when <paramref name="userId"/> may see it: the booking
    /// customer, the managing stadium owner, or an admin. Returns null otherwise, so callers
    /// cannot distinguish "not found" from "not yours".
    /// </summary>
    Task<ReservationDto?> GetByIdAsync(int id, string userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    Task<IEnumerable<ReservationDto>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDto>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<PagedResult<ReservationDto>> GetAllForAdminAsync(ReservationFilterDto? filter = null, CancellationToken cancellationToken = default);

    Task CancelAsync(int id, string userId, string? reason, CancellationToken cancellationToken = default);
    Task ConfirmAsync(int id, string managerId, CancellationToken cancellationToken = default);
    Task RejectAsync(int id, string managerId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>True when the reservation can still be cancelled by its customer right now.</summary>
    Task<bool> CanCancelAsync(int id, string customerId, CancellationToken cancellationToken = default);

    Task SubmitReviewAsync(int reservationId, string customerId, int rating, string? comment, CancellationToken cancellationToken = default);
}
