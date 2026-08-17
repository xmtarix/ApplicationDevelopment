using LebanonBasketballReservation.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace LebanonBasketballReservation.Business.DTOs;

public class CourtDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsIndoor { get; set; }
    public int Capacity { get; set; }
    public decimal HourlyPrice { get; set; }
    public CourtStatus Status { get; set; }
    public string? ImagePath { get; set; }
    public int StadiumId { get; set; }
    public string StadiumName { get; set; } = string.Empty;

    /// <summary>Number of future slots still open, for the manager's court list.</summary>
    public int AvailableSlotCount { get; set; }

    public bool IsBookable => Status == CourtStatus.Active;
    public string TypeLabel => IsIndoor ? "Indoor" : "Outdoor";
}

public class CreateCourtDto
{
    [Required(ErrorMessage = "Court name is required.")]
    [MaxLength(100, ErrorMessage = "Court name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Indoor court")]
    public bool IsIndoor { get; set; }

    [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000.")]
    public int Capacity { get; set; } = 10;

    [Range(0, 10000, ErrorMessage = "Hourly price must be between 0 and 10,000.")]
    [Display(Name = "Hourly price ($)")]
    public decimal HourlyPrice { get; set; }

    [Required]
    public int StadiumId { get; set; }
}

public class UpdateCourtDto : CreateCourtDto
{
    public int Id { get; set; }
    public CourtStatus Status { get; set; }
}

public class TimeSlotDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int CourtId { get; set; }

    public string Label => $"{StartTime:HH\\:mm} – {EndTime:HH\\:mm}";
    public double DurationHours => (EndTime - StartTime).TotalHours;
}

/// <summary>Slots for one calendar day, for the multi-day booking picker.</summary>
public class DayAvailabilityDto
{
    public DateOnly Date { get; set; }
    public List<TimeSlotDto> Slots { get; set; } = new();
    public int Count => Slots.Count;
}

public class GenerateTimeSlotsDto : IValidatableObject
{
    [Required] public int CourtId { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date), Display(Name = "From date")]
    public DateOnly FromDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date), Display(Name = "To date")]
    public DateOnly ToDate { get; set; }

    [Required, DataType(DataType.Time), Display(Name = "Opens at")]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [Required, DataType(DataType.Time), Display(Name = "Closes at")]
    public TimeOnly EndTime { get; set; } = new(21, 0);

    [Range(30, 240, ErrorMessage = "Slot duration must be between 30 and 240 minutes.")]
    [Display(Name = "Slot duration (minutes)")]
    public int SlotDurationMinutes { get; set; } = 60;

    /// <summary>Days of the week to skip, so a venue can close on e.g. Sundays.</summary>
    public List<DayOfWeek> ExcludedDays { get; set; } = new();

    /// <summary>
    /// Cross-field rules the individual attributes cannot express. Without these an
    /// inverted range silently generates nothing, which reads as a broken feature.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ToDate < FromDate)
            yield return new ValidationResult("The end date must be on or after the start date.", [nameof(ToDate)]);

        if (EndTime <= StartTime)
            yield return new ValidationResult("The closing time must be after the opening time.", [nameof(EndTime)]);

        if ((ToDate.DayNumber - FromDate.DayNumber) > 90)
            yield return new ValidationResult("Please generate at most 90 days at a time.", [nameof(ToDate)]);

        if (EndTime > StartTime && (EndTime - StartTime).TotalMinutes < SlotDurationMinutes)
            yield return new ValidationResult("The opening window is shorter than a single slot.", [nameof(SlotDurationMinutes)]);
    }
}
