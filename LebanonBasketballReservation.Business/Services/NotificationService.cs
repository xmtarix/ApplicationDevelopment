using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Business.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public NotificationService(IUnitOfWork uow, IDateTimeProvider clock)
    {
        _uow = uow;
        _clock = clock;
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType
    };

    public async Task CreateAsync(string userId, string title, string message, NotificationType type,
        int? relatedEntityId = null, string? relatedEntityType = null, CancellationToken cancellationToken = default)
    {
        // A missing recipient is not worth failing the surrounding operation over.
        if (string.IsNullOrWhiteSpace(userId)) return;

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            CreatedAt = _clock.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationDto>> GetByUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default)
        => (await _uow.Notifications.Query()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Take(take)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();

    public Task<IEnumerable<NotificationDto>> GetRecentAsync(string userId, int take = 5, CancellationToken cancellationToken = default)
        => GetByUserAsync(userId, take, cancellationToken);

    public async Task MarkReadAsync(int id, string userId, CancellationToken cancellationToken = default)
        => await _uow.Notifications.Find(n => n.Id == id && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
        => await _uow.Notifications.Find(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task DeleteAsync(int id, string userId, CancellationToken cancellationToken = default)
        => await _uow.Notifications.Find(n => n.Id == id && n.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        => await _uow.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
}
