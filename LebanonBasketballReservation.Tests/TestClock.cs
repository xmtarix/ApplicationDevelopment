using LebanonBasketballReservation.Business.Interfaces;

namespace LebanonBasketballReservation.Tests;

/// <summary>
/// A fixed, controllable clock so time-dependent rules (cancellation windows, past slots)
/// are deterministic instead of depending on when the suite runs.
/// </summary>
public class TestClock : IDateTimeProvider
{
    private readonly TimeSpan _offset;

    /// <param name="utcNow">The instant the clock reports. Defaults to a fixed date.</param>
    /// <param name="offsetHours">Venue offset from UTC, mirroring Lebanon's +3 in summer.</param>
    public TestClock(DateTime? utcNow = null, int offsetHours = 3)
    {
        UtcNow = utcNow ?? new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        _offset = TimeSpan.FromHours(offsetHours);
    }

    public DateTime UtcNow { get; set; }

    public DateTime LocalNow => UtcNow + _offset;

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public DateTime ToUtc(DateTime localDateTime) => localDateTime - _offset;

    public DateTime ToLocal(DateTime utcDateTime) => utcDateTime + _offset;
}
