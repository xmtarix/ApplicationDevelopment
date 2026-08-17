namespace LebanonBasketballReservation.Business.Interfaces;

/// <summary>
/// Supplies the current time in both UTC and the application's local (venue) time zone.
/// Reservations are booked against wall-clock time in Lebanon, but timestamps are stored
/// in UTC, so every comparison between the two must go through this abstraction.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Current instant in UTC. Use for stored timestamps (CreatedAt, CancelledAt).</summary>
    DateTime UtcNow { get; }

    /// <summary>Current wall-clock time in the venue time zone. Use for comparing against TimeSlot Date/StartTime.</summary>
    DateTime LocalNow { get; }

    /// <summary>Today's date in the venue time zone.</summary>
    DateOnly Today { get; }

    /// <summary>Converts a venue-local wall-clock instant to UTC.</summary>
    DateTime ToUtc(DateTime localDateTime);

    /// <summary>Converts a stored UTC instant to venue-local wall-clock time.</summary>
    DateTime ToLocal(DateTime utcDateTime);
}
