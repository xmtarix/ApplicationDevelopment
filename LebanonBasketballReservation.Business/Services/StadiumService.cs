using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LebanonBasketballReservation.Business.Services;

public class StadiumService : IStadiumService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<StadiumService> _logger;

    public StadiumService(
        IUnitOfWork uow,
        INotificationService notifications,
        IDateTimeProvider clock,
        ILogger<StadiumService> logger)
    {
        _uow = uow;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    private static StadiumDto Map(Stadium s, double rating = 0, int reviewCount = 0)
    {
        var images = s.Images?.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).ToList() ?? [];

        return new StadiumDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Address = s.Address,
            Phone = s.Phone,
            Email = s.Email,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            AreaId = s.AreaId,
            DistrictId = s.Area?.DistrictId ?? 0,
            GovernorateId = s.Area?.District?.GovernorateId ?? 0,
            AreaName = s.Area?.Name ?? "",
            DistrictName = s.Area?.District?.Name ?? "",
            GovernorateName = s.Area?.District?.Governorate?.Name ?? "",
            ManagerId = s.ManagerId,
            ManagerName = s.Manager?.FullName ?? "",
            ManagerEmail = s.Manager?.Email,
            CourtCount = s.Courts?.Count ?? 0,
            AverageRating = Math.Round(rating, 1),
            ReviewCount = reviewCount,
            MainImagePath = images.FirstOrDefault()?.ImagePath,
            ImagePaths = images.Select(i => i.ImagePath).ToList(),
            Courts = s.Courts?.OrderBy(c => c.Name).Select(c => new CourtDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsIndoor = c.IsIndoor,
                Capacity = c.Capacity,
                HourlyPrice = c.HourlyPrice,
                Status = c.Status,
                ImagePath = c.ImagePath,
                StadiumId = c.StadiumId,
                StadiumName = s.Name
            }).ToList() ?? [],
            Reviews = s.Reviews?.OrderByDescending(r => r.CreatedAt).Select(r => new ReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                CustomerName = r.Customer?.FullName ?? "Anonymous",
                StadiumId = r.StadiumId
            }).ToList() ?? [],
            OpeningHours = s.OpeningHours?.OrderBy(o => o.DayOfWeek).Select(o => new OpeningHourDto
            {
                DayOfWeek = o.DayOfWeek,
                OpenTime = o.OpenTime,
                CloseTime = o.CloseTime,
                IsClosed = o.IsClosed
            }).ToList() ?? []
        };
    }

    public async Task<PagedResult<StadiumDto>> SearchAsync(StadiumSearchDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Stadiums.SearchQuery(new StadiumSearchCriteria
        {
            Name = filter.Name,
            GovernorateId = filter.GovernorateId,
            DistrictId = filter.DistrictId,
            AreaId = filter.AreaId,
            IsIndoor = filter.IsIndoor,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            Status = StadiumStatus.Active,
            SortBy = filter.SortBy
        });

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        // Ratings for the whole page in one query instead of one per stadium.
        var ratings = await _uow.Stadiums.GetRatingsAsync(items.Select(s => s.Id), cancellationToken);

        return new PagedResult<StadiumDto>
        {
            Items = items.Select(s =>
            {
                var (avg, count) = ratings.GetValueOrDefault(s.Id, (0d, 0));
                return Map(s, avg, count);
            }).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<StadiumDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _uow.Stadiums.GetWithDetailsAsync(id, cancellationToken);
        if (s is null) return null;

        var ratings = await _uow.Stadiums.GetRatingsAsync([id], cancellationToken);
        var (avg, count) = ratings.GetValueOrDefault(id, (0d, 0));
        return Map(s, avg, count);
    }

    public async Task<StadiumDto> GetOwnedByIdAsync(int id, string managerId, CancellationToken cancellationToken = default)
    {
        var s = await _uow.Stadiums.GetWithDetailsAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", id);

        // Compare the manager's id, never a display name — names are not unique.
        if (s.ManagerId != managerId)
            throw new ForbiddenException("You do not manage this stadium.");

        var ratings = await _uow.Stadiums.GetRatingsAsync([id], cancellationToken);
        var (avg, count) = ratings.GetValueOrDefault(id, (0d, 0));
        return Map(s, avg, count);
    }

    public async Task<IEnumerable<StadiumDto>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default)
    {
        var stadiums = (await _uow.Stadiums.GetByManagerAsync(managerId, cancellationToken)).ToList();
        var ratings = await _uow.Stadiums.GetRatingsAsync(stadiums.Select(s => s.Id), cancellationToken);

        return stadiums.Select(s =>
        {
            var (avg, count) = ratings.GetValueOrDefault(s.Id, (0d, 0));
            return Map(s, avg, count);
        }).ToList();
    }

    public async Task<IEnumerable<StadiumDto>> GetFeaturedAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        var stadiums = (await _uow.Stadiums.GetActiveStadiumsAsync(count, cancellationToken)).ToList();
        var ratings = await _uow.Stadiums.GetRatingsAsync(stadiums.Select(s => s.Id), cancellationToken);

        return stadiums.Select(s =>
        {
            var (avg, reviews) = ratings.GetValueOrDefault(s.Id, (0d, 0));
            return Map(s, avg, reviews);
        }).ToList();
    }

    public async Task<StadiumDto> CreateAsync(CreateStadiumDto dto, string managerId, CancellationToken cancellationToken = default)
    {
        // Guard the FK explicitly so a bad AreaId surfaces as a readable message.
        if (!await _uow.Areas.ExistsAsync(a => a.Id == dto.AreaId, cancellationToken))
            throw new ValidationException("Please select a valid area.");

        var stadium = new Stadium
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? "",
            Address = dto.Address.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AreaId = dto.AreaId,
            ManagerId = managerId,
            Status = StadiumStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        await _uow.Stadiums.AddAsync(stadium, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stadium {StadiumId} submitted by manager {ManagerId}", stadium.Id, managerId);
        return (await GetByIdAsync(stadium.Id, cancellationToken))!;
    }

    public async Task<StadiumDto> UpdateAsync(UpdateStadiumDto dto, string managerId, CancellationToken cancellationToken = default)
    {
        var stadium = await _uow.Stadiums.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", dto.Id);

        if (stadium.ManagerId != managerId)
            throw new ForbiddenException("You do not manage this stadium.");

        if (!await _uow.Areas.ExistsAsync(a => a.Id == dto.AreaId, cancellationToken))
            throw new ValidationException("Please select a valid area.");

        stadium.Name = dto.Name.Trim();
        stadium.Description = dto.Description?.Trim() ?? "";
        stadium.Address = dto.Address.Trim();
        stadium.Phone = dto.Phone?.Trim();
        stadium.Email = dto.Email?.Trim();
        stadium.Latitude = dto.Latitude;
        stadium.Longitude = dto.Longitude;
        stadium.AreaId = dto.AreaId;
        stadium.UpdatedAt = _clock.UtcNow;

        _uow.Stadiums.Update(stadium);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stadium {StadiumId} updated", stadium.Id);
        return (await GetByIdAsync(stadium.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, string managerId, CancellationToken cancellationToken = default)
    {
        var stadium = await _uow.Stadiums.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", id);

        if (stadium.ManagerId != managerId)
            throw new ForbiddenException("You do not manage this stadium.");

        var hasActiveBookings = await _uow.Reservations.Find(r =>
            r.TimeSlot.Court.StadiumId == id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
            .AnyAsync(cancellationToken);

        if (hasActiveBookings)
            throw new ConflictException("This stadium has upcoming reservations and cannot be deleted.");

        _uow.Stadiums.Delete(stadium);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stadium {StadiumId} deleted", id);
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _uow.Stadiums.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", id);

        if (s.Status == StadiumStatus.Active)
            throw new ConflictException("This stadium is already active.");

        s.Status = StadiumStatus.Active;
        s.UpdatedAt = _clock.UtcNow;
        _uow.Stadiums.Update(s);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stadium {StadiumId} approved", id);

        await _notifications.CreateAsync(s.ManagerId, "Stadium approved",
            $"\"{s.Name}\" is now live and accepting reservations.",
            NotificationType.StadiumApproved, s.Id, "Stadium", cancellationToken);
    }

    public async Task RejectAsync(int id, string? reason, CancellationToken cancellationToken = default)
    {
        var s = await _uow.Stadiums.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", id);

        s.Status = StadiumStatus.Rejected;
        s.UpdatedAt = _clock.UtcNow;
        _uow.Stadiums.Update(s);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stadium {StadiumId} rejected", id);

        var suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
        await _notifications.CreateAsync(s.ManagerId, "Stadium not approved",
            $"\"{s.Name}\" was not approved.{suffix}",
            NotificationType.StadiumRejected, s.Id, "Stadium", cancellationToken);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var s = await _uow.Stadiums.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stadium", id);

        if (s.Status is StadiumStatus.Pending or StadiumStatus.Rejected)
            throw new ConflictException("Approve this stadium before changing its visibility.");

        s.Status = isActive ? StadiumStatus.Active : StadiumStatus.Inactive;
        s.UpdatedAt = _clock.UtcNow;
        _uow.Stadiums.Update(s);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<StadiumDto>> GetAllForAdminAsync(StadiumStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        // A null status means "every stadium", which the criteria expresses as no filter.
        var query = _uow.Stadiums.SearchQuery(new StadiumSearchCriteria { Status = status });

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ratings = await _uow.Stadiums.GetRatingsAsync(items.Select(s => s.Id), cancellationToken);

        return new PagedResult<StadiumDto>
        {
            Items = items.Select(s =>
            {
                var (avg, count) = ratings.GetValueOrDefault(s.Id, (0d, 0));
                return Map(s, avg, count);
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(int stadiumId, CancellationToken cancellationToken = default)
        => (await _uow.Reviews.Query()
                .Where(r => r.StadiumId == stadiumId)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken))
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                CustomerName = r.Customer?.FullName ?? "Anonymous",
                StadiumId = r.StadiumId
            })
            .ToList();
}
