using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Data.Repositories.Interfaces;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<Reservation?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByStatusAsync(ReservationStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetUpcomingAsync(DateOnly today, string? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>Confirmed reservations starting on <paramref name="targetDate"/> that have not been reminded yet.</summary>
    Task<IEnumerable<Reservation>> GetForReminderAsync(DateOnly targetDate, CancellationToken cancellationToken = default);

    /// <summary>Confirmed reservations whose end time has already passed, relative to venue-local now.</summary>
    Task<IEnumerable<Reservation>> GetCompletableAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default);

    Task<decimal> GetRevenueAsync(int? stadiumId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>Base query for reporting, with court/stadium joined for filtering.</summary>
    IQueryable<Reservation> QueryWithDetails();
}
