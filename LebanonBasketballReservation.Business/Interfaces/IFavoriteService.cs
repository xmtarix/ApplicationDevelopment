using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface IFavoriteService
{
    /// <summary>Adds or removes a favorite. Returns true when it was added.</summary>
    Task<bool> ToggleAsync(string customerId, int stadiumId, CancellationToken cancellationToken = default);

    Task<bool> IsFavoriteAsync(string customerId, int stadiumId, CancellationToken cancellationToken = default);

    /// <summary>Favorited stadium ids, for marking hearts across a whole list in one query.</summary>
    Task<HashSet<int>> GetFavoriteIdsAsync(string customerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<StadiumDto>> GetFavoritesAsync(string customerId, CancellationToken cancellationToken = default);
}
