using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Data.Repositories.Interfaces;

public interface IStadiumRepository : IRepository<Stadium>
{
    Task<IEnumerable<Stadium>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<Stadium?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Composable search query. Price bounds apply to the same court, so a stadium matches
    /// only when it actually has a court inside the requested range.
    /// </summary>
    IQueryable<Stadium> SearchQuery(StadiumSearchCriteria criteria);

    Task<IEnumerable<Stadium>> GetActiveStadiumsAsync(int? take = null, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(int stadiumId, CancellationToken cancellationToken = default);

    /// <summary>Average rating and review count for many stadiums in one query, to avoid N+1.</summary>
    Task<Dictionary<int, (double Average, int Count)>> GetRatingsAsync(IEnumerable<int> stadiumIds, CancellationToken cancellationToken = default);

    Task<bool> IsOwnedByAsync(int stadiumId, string managerId, CancellationToken cancellationToken = default);
}

/// <summary>Search filters for <see cref="IStadiumRepository.SearchQuery"/>.</summary>
public class StadiumSearchCriteria
{
    public string? Name { get; set; }
    public int? GovernorateId { get; set; }
    public int? DistrictId { get; set; }
    public int? AreaId { get; set; }
    public bool? IsIndoor { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public StadiumStatus? Status { get; set; } = StadiumStatus.Active;
    public StadiumSortOrder SortBy { get; set; } = StadiumSortOrder.Newest;
}

public enum StadiumSortOrder
{
    Newest = 0,
    NameAsc = 1,
    PriceAsc = 2,
    PriceDesc = 3,
    RatingDesc = 4
}
