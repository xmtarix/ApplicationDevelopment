namespace LebanonBasketballReservation.Data.Services;

/// <summary>
/// Exposes user counts for reporting. Lives in the Data layer because it reads the Identity
/// tables directly; the Business layer depends on Data, never the other way round.
/// </summary>
public interface IUserStatsProvider
{
    Task<UserCounts> GetUserCountsAsync(CancellationToken cancellationToken = default);
}

public readonly record struct UserCounts(int Total, int Customers, int Managers, int Admins);
