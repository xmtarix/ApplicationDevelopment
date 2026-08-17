using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Repositories.Interfaces;

namespace LebanonBasketballReservation.Data.UnitOfWork;

public interface IUnitOfWork
{
    IStadiumRepository Stadiums { get; }
    ICourtRepository Courts { get; }
    ITimeSlotRepository TimeSlots { get; }
    IReservationRepository Reservations { get; }
    IRepository<Governorate> Governorates { get; }
    IRepository<District> Districts { get; }
    IRepository<Area> Areas { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Favorite> Favorites { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<StadiumImage> StadiumImages { get; }
    IRepository<OpeningHour> OpeningHours { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> inside a database transaction, retrying on transient
    /// failures. Use for multi-step writes that must not be observed half-applied — booking a
    /// slot, for example, where the availability re-check and the insert must be atomic.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
