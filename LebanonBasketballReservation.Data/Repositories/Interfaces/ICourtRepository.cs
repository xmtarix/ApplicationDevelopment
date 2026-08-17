using LebanonBasketballReservation.Data.Entities;

namespace LebanonBasketballReservation.Data.Repositories.Interfaces;

public interface ICourtRepository : IRepository<Court>
{
    Task<IEnumerable<Court>> GetByStadiumAsync(int stadiumId, CancellationToken cancellationToken = default);
    Task<Court?> GetWithTimeSlotsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Loads a court together with its stadium, so ownership can be checked in one query.</summary>
    Task<Court?> GetWithStadiumAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>True when the court belongs to a stadium managed by <paramref name="managerId"/>.</summary>
    Task<bool> IsOwnedByAsync(int courtId, string managerId, CancellationToken cancellationToken = default);
}
