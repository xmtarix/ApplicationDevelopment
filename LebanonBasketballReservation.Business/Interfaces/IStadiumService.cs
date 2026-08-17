using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface IStadiumService
{
    Task<PagedResult<StadiumDto>> SearchAsync(StadiumSearchDto filter, CancellationToken cancellationToken = default);
    Task<StadiumDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Loads a stadium only if <paramref name="managerId"/> manages it; throws otherwise.</summary>
    Task<StadiumDto> GetOwnedByIdAsync(int id, string managerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<StadiumDto>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StadiumDto>> GetFeaturedAsync(int count = 6, CancellationToken cancellationToken = default);

    Task<StadiumDto> CreateAsync(CreateStadiumDto dto, string managerId, CancellationToken cancellationToken = default);
    Task<StadiumDto> UpdateAsync(UpdateStadiumDto dto, string managerId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string managerId, CancellationToken cancellationToken = default);

    Task ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task RejectAsync(int id, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Admin toggle between Active and Inactive for an already-approved stadium.</summary>
    Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<PagedResult<StadiumDto>> GetAllForAdminAsync(StadiumStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<IEnumerable<ReviewDto>> GetReviewsAsync(int stadiumId, CancellationToken cancellationToken = default);
}
