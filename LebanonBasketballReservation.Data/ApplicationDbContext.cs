using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Stadium> Stadiums => Set<Stadium>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<StadiumImage> StadiumImages => Set<StadiumImage>();
    public DbSet<OpeningHour> OpeningHours => Set<OpeningHour>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        builder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        builder.Entity<OpeningHour>()
            .Property(o => o.DayOfWeek)
            .HasConversion<string>();
    }
}