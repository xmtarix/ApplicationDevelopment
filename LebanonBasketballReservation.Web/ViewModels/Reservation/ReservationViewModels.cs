using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace LebanonBasketballReservation.Web.ViewModels.Reservation;

public class CreateReservationViewModel
{
    public int StadiumId { get; set; }
    public int CourtId { get; set; }
    public int TimeSlotId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes for the manager (optional)")]
    public string? Notes { get; set; }

    public StadiumDto? Stadium { get; set; }
    public CourtDto? Court { get; set; }
    public TimeSlotDto? SelectedSlot { get; set; }

    /// <summary>Available slots grouped by day, for the date/slot picker.</summary>
    public IEnumerable<DayAvailabilityDto> Calendar { get; set; } = Enumerable.Empty<DayAvailabilityDto>();

    /// <summary>Price for the selected slot, shown before the customer commits.</summary>
    public decimal EstimatedPrice => Court is null || SelectedSlot is null
        ? 0
        : Math.Round(Court.HourlyPrice * (decimal)SelectedSlot.DurationHours, 2);
}

public class CancelReservationViewModel
{
    public int Id { get; set; }

    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    [Display(Name = "Reason (optional)")]
    public string? Reason { get; set; }

    public ReservationDto? Reservation { get; set; }
}

public class ReservationListViewModel
{
    public List<ReservationDto> Upcoming { get; set; } = new();
    public List<ReservationDto> Past { get; set; } = new();
    public ReservationStatus? StatusFilter { get; set; }

    public bool IsEmpty => Upcoming.Count == 0 && Past.Count == 0;
}

public class CreateReviewViewModel
{
    public int ReservationId { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose a rating between 1 and 5 stars.")]
    public int Rating { get; set; } = 5;

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    [Display(Name = "Your review (optional)")]
    public string? Comment { get; set; }

    public ReservationDto? Reservation { get; set; }
}
