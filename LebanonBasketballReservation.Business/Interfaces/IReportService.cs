using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface IReportService
{
    /// <summary>Reservation totals and revenue for a date range, optionally scoped to one stadium.</summary>
    Task<ReservationReportDto> GetReservationReportAsync(DateTime from, DateTime to, int? stadiumId = null, CancellationToken cancellationToken = default);

    Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<ManagerDashboardStatsDto> GetManagerDashboardStatsAsync(string managerId, CancellationToken cancellationToken = default);
}
