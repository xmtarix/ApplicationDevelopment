using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.UnitOfWork;

namespace LebanonBasketballReservation.Web.BackgroundServices;

/// <summary>
/// Periodically advances reservations whose time has passed to Completed, and sends
/// reminders for tomorrow's bookings. All time comparisons use venue-local time via
/// <see cref="IDateTimeProvider"/>, since slots are wall-clock values.
/// </summary>
public class ReservationStatusService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReservationStatusService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _reminderHoursBefore;

    public ReservationStatusService(
        IServiceProvider services,
        ILogger<ReservationStatusService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;

        var minutes = configuration.GetValue("ReservationSettings:StatusCheckIntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 1440));
        _reminderHoursBefore = configuration.GetValue("ReservationSettings:ReminderHoursBefore", 24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationStatusService started; interval {Interval}", _interval);

        using var timer = new PeriodicTimer(_interval);

        // Run once at startup so a restart does not leave stale statuses for a full interval.
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let one bad pass kill the loop.
                _logger.LogError(ex, "Error in ReservationStatusService");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));

        _logger.LogInformation("ReservationStatusService stopping");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var today = clock.Today;
        var currentTime = TimeOnly.FromDateTime(clock.LocalNow);

        // 1. Complete reservations whose end time has passed.
        var completable = (await uow.Reservations.GetCompletableAsync(today, currentTime, cancellationToken)).ToList();

        foreach (var r in completable)
        {
            r.Status = ReservationStatus.Completed;
            uow.Reservations.Update(r);
        }

        if (completable.Count > 0)
        {
            await uow.SaveChangesAsync(cancellationToken);

            foreach (var r in completable)
            {
                await notifications.CreateAsync(r.CustomerId, "Reservation completed",
                    $"Your booking on {r.TimeSlot.Date:dd MMM yyyy} is complete. Tell us how it went — leave a review!",
                    NotificationType.ReservationCompleted, r.Id, "Reservation", cancellationToken);
            }

            _logger.LogInformation("Marked {Count} reservation(s) as Completed", completable.Count);
        }

        // 2. Remind customers about upcoming reservations.
        var reminderDate = DateOnly.FromDateTime(clock.LocalNow.AddHours(_reminderHoursBefore));
        var toRemind = (await uow.Reservations.GetForReminderAsync(reminderDate, cancellationToken)).ToList();

        foreach (var r in toRemind)
        {
            r.ReminderSent = true;
            uow.Reservations.Update(r);
        }

        if (toRemind.Count > 0)
        {
            // Persist the flag before sending, so a failure mid-loop cannot double-notify.
            await uow.SaveChangesAsync(cancellationToken);

            foreach (var r in toRemind)
            {
                await notifications.CreateAsync(r.CustomerId, "Upcoming reservation",
                    $"Reminder: you play at {r.TimeSlot.Court.Stadium.Name} on {r.TimeSlot.Date:dd MMM yyyy} at {r.TimeSlot.StartTime:HH\\:mm}.",
                    NotificationType.ReservationReminder, r.Id, "Reservation", cancellationToken);
            }

            _logger.LogInformation("Sent {Count} reminder(s)", toRemind.Count);
        }
    }
}
