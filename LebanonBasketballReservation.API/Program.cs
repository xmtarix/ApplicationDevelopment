using LebanonBasketballReservation.API.Middleware;
using LebanonBasketballReservation.API.Services;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Services;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, svc, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

    // Same SQLite file as the MVC app, so both halves see one set of data.
    var connectionString = SqliteConnectionString.Resolve(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        message => Log.Warning("{Message}", message));

    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Refuse to start on a default signing key rather than shipping a forgeable token.
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key is missing or shorter than 32 characters. Set it via user-secrets, " +
            "an environment variable (Jwt__Key), or appsettings.Development.json.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                // No grace period on expiry; the default 5 minutes is surprising in tests.
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

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
    builder.Services.AddScoped<ITokenService, TokenService>();

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Lebanon Basketball API",
            Version = "v1",
            Description = "REST API for the Lebanon Basketball Stadium Reservation System. " +
                          "Call POST /api/auth/login to obtain a bearer token, then authorize below."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Enter only the token — the 'Bearer ' prefix is added for you.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseMiddleware<ApiExceptionMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lebanon Basketball API v1");
        c.RoutePrefix = string.Empty;
    });

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Starting LebanonBasketballReservation.API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
