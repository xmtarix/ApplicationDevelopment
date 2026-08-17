using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.UnitOfWork;

namespace LebanonBasketballReservation.Business.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public AvailabilityService(IUnitOfWork uow, IDateTimeProvider clock)
    {
        _uow = uow;
        _clock = clock;
    }

    private static TimeSlotDto Map(TimeSlot t) => new()
    {
        Id = t.Id,
        Date = t.Date,
        StartTime = t.StartTime,
        EndTime = t.EndTime,
        IsAvailable = t.IsAvailable,
        CourtId = t.CourtId
    };

    /// <summary>A slot is only offerable while its start is still in the future.</summary>
    private bool IsStillBookable(TimeSlot t)
        => _clock.ToUtc(t.Date.ToDateTime(t.StartTime)) > _clock.UtcNow;

    public async Task<IEnumerable<TimeSlotDto>> GetAvailableSlotsAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var slots = await _uow.TimeSlots.GetAvailableAsync(courtId, date, cancellationToken);
        return slots.Where(IsStillBookable).Select(Map).ToList();
    }

    public async Task<IEnumerable<DayAvailabilityDto>> GetAvailabilityCalendarAsync(int courtId, DateOnly fromDate, int days = 14, CancellationToken cancellationToken = default)
    {
        // Never offer dates in the past, however far back the caller asks.
        if (fromDate < _clock.Today) fromDate = _clock.Today;

        var slots = await _uow.TimeSlots.GetAvailableFromAsync(courtId, fromDate, days, cancellationToken);

        return slots
            .Where(IsStillBookable)
            .GroupBy(t => t.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DayAvailabilityDto
            {
                Date = g.Key,
                Slots = g.OrderBy(t => t.StartTime).Select(Map).ToList()
            })
            .ToList();
    }

    public async Task<bool> IsSlotAvailableAsync(int timeSlotId, CancellationToken cancellationToken = default)
    {
        var slot = await _uow.TimeSlots.GetWithDetailsAsync(timeSlotId, cancellationToken);
        if (slot is null || !slot.IsAvailable || !IsStillBookable(slot)) return false;

        // A cancelled or rejected reservation leaves the slot free for rebooking.
        return slot.Reservation is null
            || slot.Reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Rejected;
    }

    public async Task<TimeSlotDto?> GetSlotByIdAsync(int timeSlotId, CancellationToken cancellationToken = default)
    {
        var slot = await _uow.TimeSlots.GetByIdAsync(timeSlotId, cancellationToken);
        return slot is null ? null : Map(slot);
    }
}
