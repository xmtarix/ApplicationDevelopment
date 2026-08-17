using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Services;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Business.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserStatsProvider _users;
    private readonly IDateTimeProvider _clock;

    public ReportService(IUnitOfWork uow, IUserStatsProvider users, IDateTimeProvider clock)
    {
        _uow = uow;
        _users = users;
        _clock = clock;
    }

    /// <summary>Statuses that represent money actually earned.</summary>
    private static bool IsRevenue(ReservationStatus s)
        => s is ReservationStatus.Confirmed or ReservationStatus.Completed;

    public async Task<ReservationReportDto> GetReservationReportAsync(DateTime from, DateTime to, int? stadiumId = null, CancellationToken cancellationToken = default)
    {
        // Make the range inclusive of the whole end day regardless of the time component supplied.
        var fromUtc = from.Date;
        var toUtc = to.Date.AddDays(1).AddTicks(-1);

        var query = _uow.Reservations.QueryWithDetails()
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc);

        if (stadiumId.HasValue)
            query = query.Where(r => r.TimeSlot.Court.StadiumId == stadiumId.Value);

        var rows = await query
            .Select(r => new
            {
                r.Status,
                r.TotalPrice,
                r.CreatedAt,
                StadiumId = r.TimeSlot.Court.StadiumId,
                StadiumName = r.TimeSlot.Court.Stadium.Name
            })
            .ToListAsync(cancellationToken);

        var report = new ReservationReportDto
        {
            From = fromUtc,
            To = toUtc,
            StadiumId = stadiumId,
            StadiumName = stadiumId.HasValue ? rows.FirstOrDefault()?.StadiumName : null,
            TotalReservations = rows.Count,
            Pending = rows.Count(r => r.Status == ReservationStatus.Pending),
            Confirmed = rows.Count(r => r.Status == ReservationStatus.Confirmed),
            Cancelled = rows.Count(r => r.Status == ReservationStatus.Cancelled),
            Rejected = rows.Count(r => r.Status == ReservationStatus.Rejected),
            Completed = rows.Count(r => r.Status == ReservationStatus.Completed),
            TotalRevenue = rows.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
        };

        report.DailyStats = rows
            .GroupBy(r => DateOnly.FromDateTime(_clock.ToLocal(r.CreatedAt)))
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatDto
            {
                Date = g.Key,
                Count = g.Count(),
                Revenue = g.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
            })
            .ToList();

        report.StadiumStats = rows
            .GroupBy(r => new { r.StadiumId, r.StadiumName })
            .Select(g => new StadiumStatDto
            {
                StadiumId = g.Key.StadiumId,
                StadiumName = g.Key.StadiumName,
                Count = g.Count(),
                Revenue = g.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
            })
            .OrderByDescending(s => s.Revenue)
            .ToList();

        return report;
    }

    public async Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var monthStartUtc = _clock.ToUtc(new DateTime(_clock.LocalNow.Year, _clock.LocalNow.Month, 1));

        var userCounts = await _users.GetUserCountsAsync(cancellationToken);

        var reservations = await _uow.Reservations.Query()
            .Select(r => new { r.Status, r.TotalPrice, r.CreatedAt, Date = r.TimeSlot.Date, StadiumName = r.TimeSlot.Court.Stadium.Name, StadiumId = r.TimeSlot.Court.StadiumId })
            .ToListAsync(cancellationToken);

        var stats = new AdminDashboardStatsDto
        {
            TotalUsers = userCounts.Total,
            TotalCustomers = userCounts.Customers,
            TotalManagers = userCounts.Managers,
            TotalStadiums = await _uow.Stadiums.CountAsync(cancellationToken: cancellationToken),
            PendingStadiums = await _uow.Stadiums.CountAsync(s => s.Status == StadiumStatus.Pending, cancellationToken),
            ActiveStadiums = await _uow.Stadiums.CountAsync(s => s.Status == StadiumStatus.Active, cancellationToken),
            TotalCourts = await _uow.Courts.CountAsync(cancellationToken: cancellationToken),
            TotalReservations = reservations.Count,
            PendingReservations = reservations.Count(r => r.Status == ReservationStatus.Pending),
            ConfirmedReservations = reservations.Count(r => r.Status == ReservationStatus.Confirmed),
            TodayReservations = reservations.Count(r => r.Date == today),
            TotalRevenue = reservations.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice),
            MonthlyRevenue = reservations.Where(r => IsRevenue(r.Status) && r.CreatedAt >= monthStartUtc).Sum(r => r.TotalPrice)
        };

        stats.RecentActivity = reservations
            .Where(r => r.CreatedAt >= _clock.UtcNow.AddDays(-14))
            .GroupBy(r => DateOnly.FromDateTime(_clock.ToLocal(r.CreatedAt)))
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatDto
            {
                Date = g.Key,
                Count = g.Count(),
                Revenue = g.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
            })
            .ToList();

        stats.TopStadiums = reservations
            .GroupBy(r => new { r.StadiumId, r.StadiumName })
            .Select(g => new StadiumStatDto
            {
                StadiumId = g.Key.StadiumId,
                StadiumName = g.Key.StadiumName,
                Count = g.Count(),
                Revenue = g.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
            })
            .OrderByDescending(s => s.Revenue)
            .Take(5)
            .ToList();

        return stats;
    }

    public async Task<ManagerDashboardStatsDto> GetManagerDashboardStatsAsync(string managerId, CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var monthStartUtc = _clock.ToUtc(new DateTime(_clock.LocalNow.Year, _clock.LocalNow.Month, 1));

        var stadiums = (await _uow.Stadiums.GetByManagerAsync(managerId, cancellationToken)).ToList();
        var stadiumIds = stadiums.Select(s => s.Id).ToList();

        var reservations = await _uow.Reservations.Query()
            .Where(r => stadiumIds.Contains(r.TimeSlot.Court.StadiumId))
            .Select(r => new { r.Status, r.TotalPrice, r.CreatedAt, Date = r.TimeSlot.Date })
            .ToListAsync(cancellationToken);

        var ratings = await _uow.Stadiums.GetRatingsAsync(stadiumIds, cancellationToken);
        var reviewCount = ratings.Values.Sum(v => v.Count);

        var stats = new ManagerDashboardStatsDto
        {
            TotalStadiums = stadiums.Count,
            ActiveStadiums = stadiums.Count(s => s.Status == StadiumStatus.Active),
            PendingStadiums = stadiums.Count(s => s.Status == StadiumStatus.Pending),
            TotalCourts = stadiums.Sum(s => s.Courts?.Count ?? 0),
            TodayReservations = reservations.Count(r => r.Date == today),
            UpcomingReservations = reservations.Count(r => r.Date >= today && r.Status == ReservationStatus.Confirmed),
            PendingReservations = reservations.Count(r => r.Status == ReservationStatus.Pending),
            TotalReservations = reservations.Count,
            CancelledReservations = reservations.Count(r => r.Status is ReservationStatus.Cancelled or ReservationStatus.Rejected),
            TotalRevenue = reservations.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice),
            // Parenthesised so the month filter applies to every revenue status, not just the last one.
            MonthlyRevenue = reservations
                .Where(r => IsRevenue(r.Status) && r.CreatedAt >= monthStartUtc)
                .Sum(r => r.TotalPrice),
            ReviewCount = reviewCount,
            AverageRating = reviewCount == 0
                ? 0
                : Math.Round(ratings.Values.Sum(v => v.Average * v.Count) / reviewCount, 1)
        };

        stats.RecentActivity = reservations
            .Where(r => r.CreatedAt >= _clock.UtcNow.AddDays(-14))
            .GroupBy(r => DateOnly.FromDateTime(_clock.ToLocal(r.CreatedAt)))
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatDto
            {
                Date = g.Key,
                Count = g.Count(),
                Revenue = g.Where(r => IsRevenue(r.Status)).Sum(r => r.TotalPrice)
            })
            .ToList();

        return stats;
    }
}
