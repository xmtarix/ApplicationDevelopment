using LebanonBasketballReservation.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace LebanonBasketballReservation.Business.DTOs;

public class ReservationDto
{
    public int Id { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public string CourtName { get; set; } = string.Empty;
    public string StadiumName { get; set; } = string.Empty;
    public string StadiumAddress { get; set; } = string.Empty;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int TimeSlotId { get; set; }
    public int CourtId { get; set; }
    public int StadiumId { get; set; }

    /// <summary>True when this reservation already carries a review.</summary>
    public bool HasReview { get; set; }

    /// <summary>True when the customer may still cancel, per the cancellation window.</summary>
    public bool CanCancel { get; set; }

    /// <summary>True when the reservation is completed and still awaiting a review.</summary>
    public bool CanReview => Status == ReservationStatus.Completed && !HasReview;

    public string StatusLabel => Status.ToString();

    /// <summary>Local start instant, for display and sorting.</summary>
    public DateTime StartsAt => Date.ToDateTime(StartTime);

    public double DurationHours => (EndTime - StartTime).TotalHours;
}

public class CreateReservationDto
{
    [Required(ErrorMessage = "Please choose a time slot.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a time slot.")]
    public int TimeSlotId { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

public class CancelReservationDto
{
    [Required] public int Id { get; set; }
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}

/// <summary>Filter and paging options for the admin reservation list.</summary>
public class ReservationFilterDto
{
    public ReservationStatus? Status { get; set; }
    public int? StadiumId { get; set; }
    public string? Search { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 20 : value;
    }
}

public class ReservationReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? StadiumId { get; set; }
    public string? StadiumName { get; set; }
    public int TotalReservations { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Cancelled { get; set; }
    public int Rejected { get; set; }
    public int Completed { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingValue => TotalReservations == 0 ? 0 : TotalRevenue / TotalReservations;

    /// <summary>Share of reservations that ended up cancelled or rejected, as a percentage.</summary>
    public double CancellationRate => TotalReservations == 0
        ? 0
        : Math.Round((Cancelled + Rejected) * 100.0 / TotalReservations, 1);

    public List<DailyStatDto> DailyStats { get; set; } = new();
    public List<StadiumStatDto> StadiumStats { get; set; } = new();
}

public class DailyStatDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class StadiumStatDto
{
    public int StadiumId { get; set; }
    public string StadiumName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}
