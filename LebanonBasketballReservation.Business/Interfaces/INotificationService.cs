using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Business.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, NotificationType type,
        int? relatedEntityId = null, string? relatedEntityType = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<NotificationDto>> GetByUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Most recent notifications, for the navbar dropdown.</summary>
    Task<IEnumerable<NotificationDto>> GetRecentAsync(string userId, int take = 5, CancellationToken cancellationToken = default);

    Task MarkReadAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
}
