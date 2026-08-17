using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LebanonBasketballReservation.Business.Services;

public class CourtService : ICourtService
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CourtService> _logger;

    public CourtService(IUnitOfWork uow, IDateTimeProvider clock, ILogger<CourtService> logger)
    {
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    private static CourtDto Map(Court c, int availableSlots = 0) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        IsIndoor = c.IsIndoor,
        Capacity = c.Capacity,
        HourlyPrice = c.HourlyPrice,
        Status = c.Status,
        ImagePath = c.ImagePath,
        StadiumId = c.StadiumId,
        StadiumName = c.Stadium?.Name ?? "",
        AvailableSlotCount = availableSlots
    };

    public async Task<IEnumerable<CourtDto>> GetByStadiumAsync(int stadiumId, CancellationToken cancellationToken = default)
    {
        var courts = (await _uow.Courts.GetByStadiumAsync(stadiumId, cancellationToken)).ToList();
        if (courts.Count == 0) return [];

        // One grouped query for all courts rather than a count per court.
        var today = _clock.Today;
        var courtIds = courts.Select(c => c.Id).ToList();
        var slotCounts = await _uow.TimeSlots.Query()
            .Where(t => courtIds.Contains(t.CourtId) && t.Date >= today && t.IsAvailable)
            .GroupBy(t => t.CourtId)
            .Select(g => new { CourtId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourtId, x => x.Count, cancellationToken);

        return courts.Select(c => Map(c, slotCounts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CourtDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _uow.Courts.GetWithStadiumAsync(id, cancellationToken);
        return c is null ? null : Map(c);
    }

    /// <summary>Throws unless the stadium exists and is managed by <paramref name="managerId"/>.</summary>
    private async Task EnsureOwnsStadiumAsync(int stadiumId, string managerId, CancellationToken cancellationToken)
    {
        if (!await _uow.Stadiums.IsOwnedByAsync(stadiumId, managerId, cancellationToken))
            throw new ForbiddenException("You do not manage this stadium.");
    }

    /// <summary>Loads a court after confirming the caller manages its stadium.</summary>
    private async Task<Court> GetOwnedCourtAsync(int courtId, string managerId, CancellationToken cancellationToken)
    {
        var court = await _uow.Courts.GetWithStadiumAsync(courtId, cancellationToken)
            ?? throw NotFoundException.For("Court", courtId);

        if (court.Stadium is null || court.Stadium.ManagerId != managerId)
            throw new ForbiddenException("You do not manage this court.");

        return court;
    }

    public async Task<CourtDto> CreateAsync(CreateCourtDto dto, string managerId, CancellationToken cancellationToken = default)
    {
        await EnsureOwnsStadiumAsync(dto.StadiumId, managerId, cancellationToken);

        var name = dto.Name.Trim();
        if (await _uow.Courts.ExistsAsync(c => c.StadiumId == dto.StadiumId && c.Name == name, cancellationToken))
            throw new ConflictException($"This stadium already has a court named \"{name}\".");

        var court = new Court
        {
            Name = name,
            Description = dto.Description?.Trim() ?? "",
            IsIndoor = dto.IsIndoor,
            Capacity = dto.Capacity,
            HourlyPrice = dto.HourlyPrice,
            StadiumId = dto.StadiumId,
            Status = CourtStatus.Active
        };

        await _uow.Courts.AddAsync(court, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Court {CourtId} created for stadium {StadiumId}", court.Id, dto.StadiumId);
        return Map(court);
    }

    public async Task<CourtDto> UpdateAsync(UpdateCourtDto dto, string managerId, CancellationToken cancellationToken = default)
    {
        var court = await GetOwnedCourtAsync(dto.Id, managerId, cancellationToken);

        var name = dto.Name.Trim();
        if (await _uow.Courts.ExistsAsync(c => c.StadiumId == court.StadiumId && c.Name == name && c.Id != court.Id, cancellationToken))
            throw new ConflictException($"This stadium already has a court named \"{name}\".");

        court.Name = name;
        court.Description = dto.Description?.Trim() ?? "";
        court.IsIndoor = dto.IsIndoor;
        court.Capacity = dto.Capacity;
        court.HourlyPrice = dto.HourlyPrice;
        court.Status = dto.Status;

        _uow.Courts.Update(court);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Court {CourtId} updated", court.Id);
        return Map(court);
    }

    public async Task DeleteAsync(int id, string managerId, CancellationToken cancellationToken = default)
    {
        var court = await GetOwnedCourtAsync(id, managerId, cancellationToken);

        // Deleting a court cascades to its slots, which would orphan live bookings.
        var hasActiveBookings = await _uow.Reservations.Find(r =>
            r.TimeSlot.CourtId == id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
            .AnyAsync(cancellationToken);

        if (hasActiveBookings)
            throw new ConflictException("This court has upcoming reservations. Cancel them first, or set the court to Inactive instead.");

        _uow.Courts.Delete(court);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Court {CourtId} deleted", id);
    }

    public async Task<int> GenerateTimeSlotsAsync(GenerateTimeSlotsDto dto, string managerId, CancellationToken cancellationToken = default)
    {
        await GetOwnedCourtAsync(dto.CourtId, managerId, cancellationToken);

        if (dto.ToDate < dto.FromDate)
            throw new ValidationException("The end date must be on or after the start date.");
        if (dto.EndTime <= dto.StartTime)
            throw new ValidationException("The closing time must be after the opening time.");
        if (dto.SlotDurationMinutes is < 30 or > 240)
            throw new ValidationException("Slot duration must be between 30 and 240 minutes.");

        // Fetch every existing key up front: one query instead of one per candidate slot.
        var existing = await _uow.TimeSlots.GetExistingKeysAsync(dto.CourtId, dto.FromDate, dto.ToDate, cancellationToken);
        var excluded = dto.ExcludedDays.ToHashSet();

        var toCreate = new List<TimeSlot>();
        for (var date = dto.FromDate; date <= dto.ToDate; date = date.AddDays(1))
        {
            if (excluded.Contains(date.DayOfWeek)) continue;

            var current = dto.StartTime;
            while (current.AddMinutes(dto.SlotDurationMinutes) <= dto.EndTime)
            {
                var end = current.AddMinutes(dto.SlotDurationMinutes);

                if (existing.Add((date, current)))
                {
                    toCreate.Add(new TimeSlot
                    {
                        CourtId = dto.CourtId,
                        Date = date,
                        StartTime = current,
                        EndTime = end,
                        IsAvailable = true
                    });
                }

                current = end;
            }
        }

        if (toCreate.Count == 0) return 0;

        await _uow.TimeSlots.AddRangeAsync(toCreate, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated {Count} time slots for court {CourtId}", toCreate.Count, dto.CourtId);
        return toCreate.Count;
    }

    public async Task<IEnumerable<TimeSlotDto>> GetTimeSlotsAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var slots = await _uow.TimeSlots.GetByCourtAndDateRangeAsync(courtId, date, date, cancellationToken);
        return slots.Select(t => new TimeSlotDto
        {
            Id = t.Id,
            Date = t.Date,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            IsAvailable = t.IsAvailable && t.Reservation is null,
            CourtId = t.CourtId
        }).ToList();
    }

    public async Task DeleteTimeSlotAsync(int slotId, string managerId, CancellationToken cancellationToken = default)
    {
        var slot = await _uow.TimeSlots.GetWithDetailsAsync(slotId, cancellationToken)
            ?? throw NotFoundException.For("Time slot", slotId);

        if (slot.Court?.Stadium is null || slot.Court.Stadium.ManagerId != managerId)
            throw new ForbiddenException("You do not manage this court.");

        if (slot.Reservation is not null &&
            slot.Reservation.Status is ReservationStatus.Pending or ReservationStatus.Confirmed)
            throw new ConflictException("This slot has an active reservation and cannot be deleted.");

        _uow.TimeSlots.Delete(slot);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ClearFutureSlotsAsync(int courtId, string managerId, CancellationToken cancellationToken = default)
    {
        await GetOwnedCourtAsync(courtId, managerId, cancellationToken);

        var today = _clock.Today;
        var removed = await _uow.TimeSlots
            .Find(t => t.CourtId == courtId && t.Date >= today && t.Reservation == null)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Cleared {Count} future slots for court {CourtId}", removed, courtId);
        return removed;
    }
}
