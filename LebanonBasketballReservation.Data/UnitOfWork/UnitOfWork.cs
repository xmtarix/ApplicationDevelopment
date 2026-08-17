using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Repositories;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
// Supplies the ExecuteAsync(Func<Task<T>>) extension on IExecutionStrategy, plus
// BeginTransactionAsync/CurrentTransaction. Without it only the interface's own
// multi-argument overload is visible and the call below fails to compile.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LebanonBasketballReservation.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IStadiumRepository Stadiums { get; }
    public ICourtRepository Courts { get; }
    public ITimeSlotRepository TimeSlots { get; }
    public IReservationRepository Reservations { get; }
    public IRepository<Governorate> Governorates { get; }
    public IRepository<District> Districts { get; }
    public IRepository<Area> Areas { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Favorite> Favorites { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<StadiumImage> StadiumImages { get; }
    public IRepository<OpeningHour> OpeningHours { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Stadiums = new StadiumRepository(context);
        Courts = new CourtRepository(context);
        TimeSlots = new TimeSlotRepository(context);
        Reservations = new ReservationRepository(context);
        Governorates = new Repository<Governorate>(context);
        Districts = new Repository<District>(context);
        Areas = new Repository<Area>(context);
        Notifications = new Repository<Notification>(context);
        Reviews = new Repository<Review>(context);
        Favorites = new Repository<Favorite>(context);
        AuditLogs = new Repository<AuditLog>(context);
        StadiumImages = new Repository<StadiumImage>(context);
        OpeningHours = new Repository<OpeningHour>(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        // A transaction may already be open (nested call, or an ambient test transaction).
        // Joining it keeps this method safe to call from anywhere.
        if (_context.Database.CurrentTransaction is not null)
            return await action();

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(async () => { await action(); return 0; }, cancellationToken);
}
