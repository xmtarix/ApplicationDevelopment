using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Repositories;

public class TimeSlotRepository : Repository<TimeSlot>, ITimeSlotRepository
{
    public TimeSlotRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TimeSlot>> GetAvailableAsync(int courtId, DateOnly date, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Where(t => t.CourtId == courtId && t.Date == date && t.IsAvailable && t.Reservation == null)
            .OrderBy(t => t.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TimeSlot>> GetAvailableFromAsync(int courtId, DateOnly fromDate, int days, CancellationToken cancellationToken = default)
    {
        var toDate = fromDate.AddDays(Math.Max(0, days));
        return await _dbSet.AsNoTracking()
            .Where(t => t.CourtId == courtId && t.Date >= fromDate && t.Date <= toDate
                     && t.IsAvailable && t.Reservation == null)
            .OrderBy(t => t.Date).ThenBy(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TimeSlot>> GetByCourtAndDateRangeAsync(int courtId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Include(t => t.Reservation)
            .Where(t => t.CourtId == courtId && t.Date >= from && t.Date <= to)
            .OrderBy(t => t.Date).ThenBy(t => t.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<TimeSlot?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(t => t.Court).ThenInclude(c => c.Stadium)
            .Include(t => t.Reservation)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<bool> IsSlotAvailableAsync(int courtId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSlotId = null, CancellationToken cancellationToken = default)
    {
        // A slot is blocked when an overlapping slot on the same court already carries a
        // reservation that still holds the court (Pending or Confirmed). Cancelled and
        // rejected reservations release their slot and must not block a rebooking.
        var query = _dbSet
            .Where(t => t.CourtId == courtId
                     && t.Date == date
                     && t.StartTime < end && t.EndTime > start
                     && t.Reservation != null
                     && (t.Reservation.Status == ReservationStatus.Pending
                      || t.Reservation.Status == ReservationStatus.Confirmed
                      || t.Reservation.Status == ReservationStatus.Completed));

        if (excludeSlotId.HasValue)
            query = query.Where(t => t.Id != excludeSlotId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(int timeSlotId, CancellationToken cancellationToken = default)
    {
        // Conditional UPDATE ... WHERE IsAvailable = 1 executed by the database, so exactly one
        // of N concurrent callers sees a non-zero row count. ExecuteUpdate bypasses the change
        // tracker, which is what makes the check-and-set a single atomic statement.
        var rowsAffected = await _dbSet
            .Where(t => t.Id == timeSlotId && t.IsAvailable)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsAvailable, false), cancellationToken);

        return rowsAffected > 0;
    }

    public async Task ReleaseAsync(int timeSlotId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(t => t.Id == timeSlotId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsAvailable, true), cancellationToken);

    public async Task<HashSet<(DateOnly Date, TimeOnly StartTime)>> GetExistingKeysAsync(int courtId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var keys = await _dbSet.AsNoTracking()
            .Where(t => t.CourtId == courtId && t.Date >= from && t.Date <= to)
            .Select(t => new { t.Date, t.StartTime })
            .ToListAsync(cancellationToken);

        return keys.Select(k => (k.Date, k.StartTime)).ToHashSet();
    }
}
