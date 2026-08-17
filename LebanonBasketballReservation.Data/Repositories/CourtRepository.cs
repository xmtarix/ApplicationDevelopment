using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Repositories;

public class CourtRepository : Repository<Court>, ICourtRepository
{
    public CourtRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Court>> GetByStadiumAsync(int stadiumId, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Include(c => c.Stadium)
            .Where(c => c.StadiumId == stadiumId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<Court?> GetWithTimeSlotsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(c => c.TimeSlots)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Court?> GetWithStadiumAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(c => c.Stadium)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> IsOwnedByAsync(int courtId, string managerId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(c => c.Id == courtId && c.Stadium.ManagerId == managerId, cancellationToken);
}
