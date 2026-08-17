using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Seed;
using LebanonBasketballReservation.Data.Services;
using LebanonBasketballReservation.Data.UnitOfWork;
using LebanonBasketballReservation.Web.BackgroundServices;
using LebanonBasketballReservation.Web.Middleware;
using LebanonBasketballReservation.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LebanonBasketballReservation.Web");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

    var connectionString = SqliteConnectionString.Resolve(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        message => Log.Warning("{Message}", message));

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(connectionString);

        if (builder.Environment.IsDevelopment())
            options.EnableDetailedErrors();
    });

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;

        // Slow down credential stuffing without locking people out for good.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // Deactivated accounts must lose access on their next request, not at cookie expiry.
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        options.ValidationInterval = TimeSpan.FromMinutes(5));

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IUserStatsProvider, UserStatsProvider>();
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IStadiumService, StadiumService>();
    builder.Services.AddScoped<ICourtService, CourtService>();
    builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IReservationService, ReservationService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IFavoriteService, FavoriteService>();
    builder.Services.AddScoped<IFileService, FileService>();

    builder.Services.AddHostedService<ReservationStatusService>();

    builder.Services.AddControllersWithViews(options =>
    {
        // Antiforgery on every unsafe verb by default, so a missing attribute cannot open a hole.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    });

    builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

    var app = builder.Build();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context, userManager, roleManager, app.Configuration);
            Log.Information("Database migrated and seeded");
        }
        catch (Exception ex)
        {
            // Starting without a database would only fail later, more confusingly.
            Log.Fatal(ex, "Database migration/seeding failed");
            throw;
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // Runs inside the exception handler so it can render a friendly page rather than redirect.
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
    app.UseResponseCompression();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed so the integration test host can reference this entry point.</summary>
public partial class Program { }
