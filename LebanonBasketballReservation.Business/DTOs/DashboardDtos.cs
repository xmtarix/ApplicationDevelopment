namespace LebanonBasketballReservation.Business.DTOs;

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalManagers { get; set; }
    public int TotalStadiums { get; set; }
    public int PendingStadiums { get; set; }
    public int ActiveStadiums { get; set; }
    public int TotalCourts { get; set; }
    public int TotalReservations { get; set; }
    public int PendingReservations { get; set; }
    public int ConfirmedReservations { get; set; }
    public int TodayReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }

    public List<DailyStatDto> RecentActivity { get; set; } = new();
    public List<StadiumStatDto> TopStadiums { get; set; } = new();
}

public class ManagerDashboardStatsDto
{
    public int TotalStadiums { get; set; }
    public int ActiveStadiums { get; set; }
    public int PendingStadiums { get; set; }
    public int TotalCourts { get; set; }
    public int TodayReservations { get; set; }
    public int UpcomingReservations { get; set; }
    public int PendingReservations { get; set; }
    public int TotalReservations { get; set; }
    public int CancelledReservations { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public List<DailyStatDto> RecentActivity { get; set; } = new();
}
