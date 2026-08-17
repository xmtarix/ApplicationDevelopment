using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface ICourtService
{
    Task<IEnumerable<CourtDto>> GetByStadiumAsync(int stadiumId, CancellationToken cancellationToken = default);
    Task<CourtDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CourtDto> CreateAsync(CreateCourtDto dto, string managerId, CancellationToken cancellationToken = default);
    Task<CourtDto> UpdateAsync(UpdateCourtDto dto, string managerId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string managerId, CancellationToken cancellationToken = default);

    /// <summary>Bulk-creates slots, skipping any that already exist. Returns how many were added.</summary>
    Task<int> GenerateTimeSlotsAsync(GenerateTimeSlotsDto dto, string managerId, CancellationToken cancellationToken = default);

    /// <summary>All slots for a court on a date, booked ones included, for the manager view.</summary>
    Task<IEnumerable<TimeSlotDto>> GetTimeSlotsAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default);

    Task DeleteTimeSlotAsync(int slotId, string managerId, CancellationToken cancellationToken = default);

    /// <summary>Removes every unbooked future slot for a court. Returns how many were removed.</summary>
    Task<int> ClearFutureSlotsAsync(int courtId, string managerId, CancellationToken cancellationToken = default);
}
