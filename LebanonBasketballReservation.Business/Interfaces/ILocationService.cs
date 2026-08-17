using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<GovernorateDto>> GetGovernoratesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DistrictDto>> GetDistrictsByGovernorateAsync(int governorateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AreaDto>> GetAreasByDistrictAsync(int districtId, CancellationToken cancellationToken = default);

    /// <summary>The full hierarchy for the admin locations screen.</summary>
    Task<IEnumerable<GovernorateTreeDto>> GetHierarchyAsync(CancellationToken cancellationToken = default);

    Task<GovernorateDto> CreateGovernorateAsync(string name, string nameAr, CancellationToken cancellationToken = default);
    Task<DistrictDto> CreateDistrictAsync(string name, string nameAr, int governorateId, CancellationToken cancellationToken = default);
    Task<AreaDto> CreateAreaAsync(string name, string nameAr, int districtId, CancellationToken cancellationToken = default);

    Task DeleteGovernorateAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteDistrictAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAreaAsync(int id, CancellationToken cancellationToken = default);
}
