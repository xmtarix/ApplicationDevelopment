using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Repositories;

public class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    public ReservationRepository(ApplicationDbContext context) : base(context) { }

    public IQueryable<Reservation> QueryWithDetails()
        => _dbSet.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.TimeSlot).ThenInclude(t => t.Court).ThenInclude(c => c.Stadium);

    public async Task<IEnumerable<Reservation>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => await QueryWithDetails()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.TimeSlot.Date).ThenByDescending(r => r.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default)
        => await QueryWithDetails()
            .Where(r => r.TimeSlot.Court.Stadium.ManagerId == managerId)
            .OrderByDescending(r => r.TimeSlot.Date).ThenByDescending(r => r.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<Reservation?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.Customer)
            .Include(r => r.Review)
            .Include(r => r.TimeSlot).ThenInclude(t => t.Court).ThenInclude(c => c.Stadium)
                .ThenInclude(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByStatusAsync(ReservationStatus status, CancellationToken cancellationToken = default)
        => await QueryWithDetails()
            .Where(r => r.Status == status)
            .OrderBy(r => r.TimeSlot.Date).ThenBy(r => r.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Reservation>> GetUpcomingAsync(DateOnly today, string? customerId = null, CancellationToken cancellationToken = default)
    {
        var query = QueryWithDetails()
            .Where(r => r.TimeSlot.Date >= today
                     && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending));

        if (customerId is not null)
            query = query.Where(r => r.CustomerId == customerId);

        return await query.OrderBy(r => r.TimeSlot.Date).ThenBy(r => r.TimeSlot.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetForReminderAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.Customer)
            .Include(r => r.TimeSlot).ThenInclude(t => t.Court).ThenInclude(c => c.Stadium)
            .Where(r => r.Status == ReservationStatus.Confirmed
                     && !r.ReminderSent
                     && r.TimeSlot.Date == targetDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Reservation>> GetCompletableAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.TimeSlot).ThenInclude(t => t.Court).ThenInclude(c => c.Stadium)
            .Where(r => r.Status == ReservationStatus.Confirmed
                     && (r.TimeSlot.Date < today
                      || (r.TimeSlot.Date == today && r.TimeSlot.EndTime <= currentTime)))
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetRevenueAsync(int? stadiumId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r => r.Status == ReservationStatus.Confirmed
                                   || r.Status == ReservationStatus.Completed);

        if (stadiumId.HasValue)
            query = query.Where(r => r.TimeSlot.Court.StadiumId == stadiumId.Value);
        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        // SumAsync over an empty set returns 0 for a non-nullable decimal projection.
        return await query.SumAsync(r => r.TotalPrice, cancellationToken);
    }
}
