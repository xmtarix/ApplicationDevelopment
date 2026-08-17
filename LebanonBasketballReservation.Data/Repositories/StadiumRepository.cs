using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data.Repositories;

public class StadiumRepository : Repository<Stadium>, IStadiumRepository
{
    public StadiumRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Stadium>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Where(s => s.ManagerId == managerId)
            .Include(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .Include(s => s.Courts)
            .Include(s => s.Images)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Stadium?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Include(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .Include(s => s.Manager)
            .Include(s => s.Courts)
            .Include(s => s.Images)
            .Include(s => s.OpeningHours)
            .Include(s => s.Reviews).ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public IQueryable<Stadium> SearchQuery(StadiumSearchCriteria criteria)
    {
        var query = _dbSet.AsNoTracking()
            .Include(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .Include(s => s.Courts)
            .Include(s => s.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            var name = criteria.Name.Trim();
            query = query.Where(s => s.Name.Contains(name) || s.Address.Contains(name));
        }

        if (criteria.GovernorateId.HasValue)
            query = query.Where(s => s.Area.District.GovernorateId == criteria.GovernorateId.Value);
        if (criteria.DistrictId.HasValue)
            query = query.Where(s => s.Area.DistrictId == criteria.DistrictId.Value);
        if (criteria.AreaId.HasValue)
            query = query.Where(s => s.AreaId == criteria.AreaId.Value);

        // Court-level filters must all hold for a SINGLE court, otherwise a stadium with a
        // cheap outdoor court and an expensive indoor one would match contradictory filters.
        var min = criteria.MinPrice;
        var max = criteria.MaxPrice;
        var indoor = criteria.IsIndoor;

        if (indoor.HasValue || min.HasValue || max.HasValue)
        {
            query = query.Where(s => s.Courts.Any(c =>
                c.Status == CourtStatus.Active
                && (!indoor.HasValue || c.IsIndoor == indoor.Value)
                && (!min.HasValue || c.HourlyPrice >= min.Value)
                && (!max.HasValue || c.HourlyPrice <= max.Value)));
        }

        if (criteria.Status.HasValue)
            query = query.Where(s => s.Status == criteria.Status.Value);

        query = criteria.SortBy switch
        {
            StadiumSortOrder.NameAsc => query.OrderBy(s => s.Name),
            StadiumSortOrder.PriceAsc => query.OrderBy(s => s.Courts.Min(c => (decimal?)c.HourlyPrice) ?? 0),
            StadiumSortOrder.PriceDesc => query.OrderByDescending(s => s.Courts.Min(c => (decimal?)c.HourlyPrice) ?? 0),
            StadiumSortOrder.RatingDesc => query.OrderByDescending(s => s.Reviews.Any() ? s.Reviews.Average(r => (double)r.Rating) : 0),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };

        // A deterministic tiebreaker keeps paging stable when the sort key repeats.
        return query is IOrderedQueryable<Stadium> ordered ? ordered.ThenBy(s => s.Id) : query;
    }

    public async Task<IEnumerable<Stadium>> GetActiveStadiumsAsync(int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Where(s => s.Status == StadiumStatus.Active)
            .Include(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .Include(s => s.Images)
            .Include(s => s.Courts)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<double> GetAverageRatingAsync(int stadiumId, CancellationToken cancellationToken = default)
    {
        // Average over an empty set would throw for a non-nullable projection, hence the cast.
        var average = await _context.Reviews
            .Where(r => r.StadiumId == stadiumId)
            .AverageAsync(r => (double?)r.Rating, cancellationToken);

        return average ?? 0;
    }

    public async Task<Dictionary<int, (double Average, int Count)>> GetRatingsAsync(IEnumerable<int> stadiumIds, CancellationToken cancellationToken = default)
    {
        var ids = stadiumIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, (double, int)>();

        var grouped = await _context.Reviews
            .Where(r => ids.Contains(r.StadiumId))
            .GroupBy(r => r.StadiumId)
            .Select(g => new { StadiumId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.StadiumId, g => (g.Average, g.Count));
    }

    public async Task<bool> IsOwnedByAsync(int stadiumId, string managerId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(s => s.Id == stadiumId && s.ManagerId == managerId, cancellationToken);
}
