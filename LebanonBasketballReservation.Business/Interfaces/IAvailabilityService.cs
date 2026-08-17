using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface IAvailabilityService
{
    /// <summary>Bookable slots for a court on a date. Slots already started are excluded.</summary>
    Task<IEnumerable<TimeSlotDto>> GetAvailableSlotsAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Bookable slots grouped by date, for a multi-day picker.</summary>
    Task<IEnumerable<DayAvailabilityDto>> GetAvailabilityCalendarAsync(int courtId, DateOnly fromDate, int days = 14, CancellationToken cancellationToken = default);

    Task<bool> IsSlotAvailableAsync(int timeSlotId, CancellationToken cancellationToken = default);
    Task<TimeSlotDto?> GetSlotByIdAsync(int timeSlotId, CancellationToken cancellationToken = default);
}
