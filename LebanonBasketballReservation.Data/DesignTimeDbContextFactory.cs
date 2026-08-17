using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LebanonBasketballReservation.Data;

/// <summary>
/// Used by `dotnet ef` at design time (migrations, scaffolding). The running app builds its
/// own context from Program.cs; this only needs a connection string good enough for the tools.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var webProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../LebanonBasketballReservation.Web");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(webProjectPath) ? webProjectPath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = SqliteConnectionString.Resolve(
            configuration.GetConnectionString("DefaultConnection"));

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
