using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Business.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public FavoriteService(IUnitOfWork uow, IDateTimeProvider clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<bool> ToggleAsync(string customerId, int stadiumId, CancellationToken cancellationToken = default)
    {
        if (!await _uow.Stadiums.ExistsAsync(s => s.Id == stadiumId, cancellationToken))
            throw NotFoundException.For("Stadium", stadiumId);

        var existing = await _uow.Favorites
            .Find(f => f.CustomerId == customerId && f.StadiumId == stadiumId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            _uow.Favorites.Delete(existing);
            await _uow.SaveChangesAsync(cancellationToken);
            return false; // removed
        }

        await _uow.Favorites.AddAsync(new Favorite
        {
            CustomerId = customerId,
            StadiumId = stadiumId,
            CreatedAt = _clock.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return true; // added
    }

    public async Task<bool> IsFavoriteAsync(string customerId, int stadiumId, CancellationToken cancellationToken = default)
        => await _uow.Favorites.ExistsAsync(f => f.CustomerId == customerId && f.StadiumId == stadiumId, cancellationToken);

    public async Task<HashSet<int>> GetFavoriteIdsAsync(string customerId, CancellationToken cancellationToken = default)
        => (await _uow.Favorites.Query()
                .Where(f => f.CustomerId == customerId)
                .Select(f => f.StadiumId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

    public async Task<IEnumerable<StadiumDto>> GetFavoritesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var stadiums = await _uow.Favorites.Query()
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Stadium)
            .Include(s => s.Area).ThenInclude(a => a.District).ThenInclude(d => d.Governorate)
            .Include(s => s.Courts)
            .Include(s => s.Images)
            .ToListAsync(cancellationToken);

        var ratings = await _uow.Stadiums.GetRatingsAsync(stadiums.Select(s => s.Id), cancellationToken);

        return stadiums.Select(s =>
        {
            var (avg, count) = ratings.GetValueOrDefault(s.Id, (0d, 0));
            var images = s.Images?.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).ToList() ?? [];

            return new StadiumDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Address = s.Address,
                Status = s.Status,
                AreaId = s.AreaId,
                AreaName = s.Area?.Name ?? "",
                DistrictName = s.Area?.District?.Name ?? "",
                GovernorateName = s.Area?.District?.Governorate?.Name ?? "",
                CourtCount = s.Courts?.Count ?? 0,
                AverageRating = Math.Round(avg, 1),
                ReviewCount = count,
                MainImagePath = images.FirstOrDefault()?.ImagePath,
                Courts = s.Courts?.Select(c => new CourtDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    HourlyPrice = c.HourlyPrice,
                    IsIndoor = c.IsIndoor,
                    Status = c.Status,
                    StadiumId = c.StadiumId,
                    StadiumName = s.Name
                }).ToList() ?? []
            };
        }).ToList();
    }
}
