using LebanonBasketballReservation.Data.Entities;

namespace LebanonBasketballReservation.Data.Repositories.Interfaces;

public interface ITimeSlotRepository : IRepository<TimeSlot>
{
    Task<IEnumerable<TimeSlot>> GetAvailableAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Available slots from <paramref name="fromDate"/> onward, for calendar views.</summary>
    Task<IEnumerable<TimeSlot>> GetAvailableFromAsync(int courtId, DateOnly fromDate, int days, CancellationToken cancellationToken = default);

    Task<IEnumerable<TimeSlot>> GetByCourtAndDateRangeAsync(int courtId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Loads a slot together with its court, stadium and any existing reservation.</summary>
    Task<TimeSlot?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>True when no reservation overlaps the given window on that court.</summary>
    Task<bool> IsSlotAvailableAsync(int courtId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSlotId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically flips a slot from available to unavailable. Returns true only for the caller
    /// that won the race; concurrent callers get false. This is the guard against double-booking.
    /// </summary>
    Task<bool> TryClaimAsync(int timeSlotId, CancellationToken cancellationToken = default);

    /// <summary>Marks a slot available again after a cancellation or rejection.</summary>
    Task ReleaseAsync(int timeSlotId, CancellationToken cancellationToken = default);

    /// <summary>Existing (CourtId, Date, StartTime) keys in a range, for bulk generation without N queries.</summary>
    Task<HashSet<(DateOnly Date, TimeOnly StartTime)>> GetExistingKeysAsync(int courtId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
