using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Business.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    /// <summary>Bootstrap icon name matching the notification kind.</summary>
    public string Icon => Type switch
    {
        NotificationType.ReservationConfirmed => "bi-check-circle-fill",
        NotificationType.ReservationCancelled => "bi-x-circle-fill",
        NotificationType.ReservationRejected => "bi-slash-circle-fill",
        NotificationType.ReservationReminder => "bi-alarm-fill",
        NotificationType.ReservationCompleted => "bi-flag-fill",
        NotificationType.StadiumApproved => "bi-patch-check-fill",
        NotificationType.StadiumRejected => "bi-patch-exclamation-fill",
        _ => "bi-bell-fill"
    };

    /// <summary>Bootstrap text colour class matching the notification kind.</summary>
    public string ColorClass => Type switch
    {
        NotificationType.ReservationConfirmed or NotificationType.StadiumApproved => "text-success",
        NotificationType.ReservationCancelled or NotificationType.ReservationRejected or NotificationType.StadiumRejected => "text-danger",
        NotificationType.ReservationReminder => "text-warning",
        NotificationType.ReservationCompleted => "text-primary",
        _ => "text-secondary"
    };
}
