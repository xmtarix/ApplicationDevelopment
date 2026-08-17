using LebanonBasketballReservation.Business.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LebanonBasketballReservation.Business.Services;

/// <summary>
/// Resolves the venue time zone once at construction. Falls back gracefully when the
/// configured id is unknown on the host OS (Windows and Linux/macOS use different
/// time zone databases), so the app never fails to start over a time zone name.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _timeZone;

    public DateTimeProvider(IConfiguration config)
    {
        var configured = config["ReservationSettings:TimeZone"];
        _timeZone = ResolveTimeZone(configured);
    }

    public DateTimeProvider(TimeZoneInfo timeZone) => _timeZone = timeZone;

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        // Try the configured id, then the well-known aliases for Lebanon on each OS,
        // then give up and use the host's local zone.
        foreach (var candidate in new[] { id, "Asia/Beirut", "Middle East Standard Time" })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public DateTime ToUtc(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        // A wall-clock time inside the spring-forward gap does not exist locally.
        // Shift past the gap rather than throwing — a booking must still resolve to an instant.
        if (_timeZone.IsInvalidTime(unspecified))
            unspecified = unspecified.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }

    public DateTime ToLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), _timeZone);
}
