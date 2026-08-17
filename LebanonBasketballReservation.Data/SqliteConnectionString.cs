namespace LebanonBasketballReservation.Data;

/// <summary>
/// Picks the SQLite connection string to run with.
///
/// This application targets SQLite, but a machine may still carry a SQL Server value from an
/// earlier configuration — in user-secrets or a ConnectionStrings__DefaultConnection environment
/// variable, both of which outrank appsettings.json. Rather than crash on a setting the developer
/// may not know exists, an incompatible value is ignored in favour of the working default and the
/// substitution is logged.
/// </summary>
public static class SqliteConnectionString
{
    /// <summary>The database file, resolved relative to the project directory at run time.</summary>
    public const string Default = "Data Source=../LebanonBasketball.db";

    /// <summary>
    /// Returns <paramref name="configured"/> when it is a usable SQLite string, otherwise
    /// <see cref="Default"/>. <paramref name="warn"/> is invoked when a value is discarded.
    /// </summary>
    public static string Resolve(string? configured, Action<string>? warn = null)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return Default;

        if (IsSqlServerStyle(configured))
        {
            warn?.Invoke(
                $"Ignoring ConnectionStrings:DefaultConnection ('{configured}') because it is a SQL Server " +
                $"connection string and this application uses SQLite. Falling back to '{Default}'. " +
                "To silence this, clear the stale override: dotnet user-secrets remove " +
                "\"ConnectionStrings:DefaultConnection\" --project <project>, or unset the " +
                "ConnectionStrings__DefaultConnection environment variable.");

            return Default;
        }

        return configured;
    }

    /// <summary>
    /// True for connection strings aimed at SQL Server. "Server=" alone is not decisive —
    /// SQLite accepts no such keyword, so its presence means the string was written for
    /// another provider.
    /// </summary>
    private static bool IsSqlServerStyle(string connectionString)
        => connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase);
}
