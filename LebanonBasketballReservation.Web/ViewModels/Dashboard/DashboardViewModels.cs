using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Web.ViewModels.Dashboard;

public class ManagerDashboardViewModel
{
    public ManagerDashboardStatsDto Stats { get; set; } = new();
    public IEnumerable<StadiumDto> MyStadiums { get; set; } = Enumerable.Empty<StadiumDto>();
    public IEnumerable<ReservationDto> PendingRequests { get; set; } = Enumerable.Empty<ReservationDto>();
    public IEnumerable<ReservationDto> RecentReservations { get; set; } = Enumerable.Empty<ReservationDto>();
}

public class CustomerDashboardViewModel
{
    public IEnumerable<ReservationDto> UpcomingReservations { get; set; } = Enumerable.Empty<ReservationDto>();
    public IEnumerable<ReservationDto> PastReservations { get; set; } = Enumerable.Empty<ReservationDto>();
    public IEnumerable<ReservationDto> AwaitingReview { get; set; } = Enumerable.Empty<ReservationDto>();
    public IEnumerable<StadiumDto> Favorites { get; set; } = Enumerable.Empty<StadiumDto>();

    public int TotalReservations { get; set; }
    public int CompletedReservations { get; set; }
    public decimal TotalSpent { get; set; }
    public int UnreadNotifications { get; set; }
}
