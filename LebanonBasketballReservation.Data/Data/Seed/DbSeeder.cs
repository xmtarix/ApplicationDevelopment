using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LebanonBasketballReservation.Data.Seed;

public static class DbSeeder
{
    /// <summary>
    /// Seed passwords come from configuration when present, so a deployed instance is not
    /// created with the documented demo credentials.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration? configuration = null)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, configuration);
        await SeedLocationsAsync(context);
        await SeedStadiumsAsync(context, userManager);
        await SeedTimeSlotsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration? configuration)
    {
        await EnsureUserAsync(userManager, configuration, "admin@lbbasket.com", "Admin@123!",
            "System", "Admin", RoleNames.Admin, "SeedUsers:AdminPassword");

        await EnsureUserAsync(userManager, configuration, "manager@lbbasket.com", "Manager@123!",
            "Ahmad", "Khalil", RoleNames.StadiumManager, "SeedUsers:ManagerPassword", "+961 3 111222");

        await EnsureUserAsync(userManager, configuration, "customer@lbbasket.com", "Customer@123!",
            "Rami", "Haddad", RoleNames.Customer, "SeedUsers:CustomerPassword", "+961 70 333444");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration,
        string email,
        string fallbackPassword,
        string firstName,
        string lastName,
        string role,
        string configKey,
        string? phone = null)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            EmailConfirmed = true,
            IsActive = true
        };

        var password = configuration?[configKey] ?? fallbackPassword;
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            // Surface the reason rather than silently ending up with no seed accounts.
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create seed user '{email}': {errors}");
        }
    }

    private static async Task SeedLocationsAsync(ApplicationDbContext context)
    {
        if (await context.Governorates.AnyAsync()) return;

        var beirut        = new Governorate { Name = "Beirut",          NameAr = "بيروت" };
        var mountLebanon  = new Governorate { Name = "Mount Lebanon",   NameAr = "جبل لبنان" };
        var northLebanon  = new Governorate { Name = "North Lebanon",   NameAr = "شمال لبنان" };
        var southLebanon  = new Governorate { Name = "South Lebanon",   NameAr = "جنوب لبنان" };
        var beqaa         = new Governorate { Name = "Beqaa",           NameAr = "البقاع" };
        var nabatieh      = new Governorate { Name = "Nabatieh",        NameAr = "النبطية" };
        var akkar         = new Governorate { Name = "Akkar",           NameAr = "عكار" };
        var baalbekHermel = new Governorate { Name = "Baalbek-Hermel",  NameAr = "بعلبك الهرمل" };

        context.Governorates.AddRange(beirut, mountLebanon, northLebanon, southLebanon,
                                      beqaa, nabatieh, akkar, baalbekHermel);

        var beirutDistrict = new District { Name = "Beirut",    NameAr = "بيروت",   Governorate = beirut };
        var metn           = new District { Name = "Metn",      NameAr = "المتن",   Governorate = mountLebanon };
        var kesrouan       = new District { Name = "Kesrouan",  NameAr = "كسروان",  Governorate = mountLebanon };
        var chouf          = new District { Name = "Chouf",     NameAr = "الشوف",   Governorate = mountLebanon };
        var aley           = new District { Name = "Aley",      NameAr = "عاليه",   Governorate = mountLebanon };
        var baabda         = new District { Name = "Baabda",    NameAr = "بعبدا",   Governorate = mountLebanon };
        var tripoli        = new District { Name = "Tripoli",   NameAr = "طرابلس",  Governorate = northLebanon };
        var zgharta        = new District { Name = "Zgharta",   NameAr = "زغرتا",   Governorate = northLebanon };
        var koura          = new District { Name = "Koura",     NameAr = "الكورة",  Governorate = northLebanon };
        var saida          = new District { Name = "Saida",     NameAr = "صيدا",    Governorate = southLebanon };
        var tyre           = new District { Name = "Tyre",      NameAr = "صور",     Governorate = southLebanon };
        var zahle          = new District { Name = "Zahle",     NameAr = "زحلة",    Governorate = beqaa };
        var nabatiehDist   = new District { Name = "Nabatieh",  NameAr = "النبطية", Governorate = nabatieh };

        context.Districts.AddRange(beirutDistrict, metn, kesrouan, chouf, aley, baabda,
                                   tripoli, zgharta, koura, saida, tyre, zahle, nabatiehDist);

        var areas = new[]
        {
            new Area { Name = "Hamra",       NameAr = "الحمراء",   District = beirutDistrict },
            new Area { Name = "Achrafieh",   NameAr = "الأشرفية",  District = beirutDistrict },
            new Area { Name = "Verdun",      NameAr = "فردان",     District = beirutDistrict },
            new Area { Name = "Raoucheh",    NameAr = "الروشة",    District = beirutDistrict },
            new Area { Name = "Badaro",      NameAr = "بدارو",     District = beirutDistrict },
            new Area { Name = "Dbayeh",      NameAr = "ضبيه",      District = metn },
            new Area { Name = "Jdeideh",     NameAr = "الجديدة",   District = metn },
            new Area { Name = "Broumana",    NameAr = "برمانا",    District = metn },
            new Area { Name = "Antelias",    NameAr = "أنطلياس",   District = metn },
            new Area { Name = "Jounieh",     NameAr = "جونية",     District = kesrouan },
            new Area { Name = "Kaslik",      NameAr = "الكسليك",   District = kesrouan },
            new Area { Name = "Beit Mery",   NameAr = "بيت مري",   District = metn },
            new Area { Name = "Hazmieh",     NameAr = "الحازمية",  District = baabda },
            new Area { Name = "Aley Centre", NameAr = "عاليه",     District = aley },
            new Area { Name = "Damour",      NameAr = "الدامور",   District = chouf },
            new Area { Name = "El Mina",     NameAr = "الميناء",   District = tripoli },
            new Area { Name = "Azmi",        NameAr = "عزمي",      District = tripoli },
            new Area { Name = "Chekka",      NameAr = "شكا",       District = koura },
            new Area { Name = "Saida Centre",NameAr = "صيدا",      District = saida },
            new Area { Name = "Tyre Centre", NameAr = "صور",       District = tyre },
            new Area { Name = "Zahle Centre",NameAr = "زحلة",      District = zahle },
            new Area { Name = "Nabatieh Centre", NameAr = "النبطية", District = nabatiehDist }
        };

        context.Areas.AddRange(areas);
        await context.SaveChangesAsync();
    }

    private static async Task SeedStadiumsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Stadiums.AnyAsync()) return;

        var manager = await userManager.FindByEmailAsync("manager@lbbasket.com");
        if (manager is null) return;

        async Task<Area?> AreaByName(string name)
            => await context.Areas.FirstOrDefaultAsync(a => a.Name == name);

        var hamra = await AreaByName("Hamra");
        var jounieh = await AreaByName("Jounieh");
        var achrafieh = await AreaByName("Achrafieh");
        var mina = await AreaByName("El Mina");
        if (hamra is null || jounieh is null || achrafieh is null || mina is null) return;

        var stadium1 = new Stadium
        {
            Name = "Beirut Basketball Arena",
            Description = "Modern indoor basketball facility in the heart of Beirut with professional-grade hardwood courts, changing rooms and floodlighting.",
            Address = "Hamra Street, Beirut",
            Phone = "+961 1 234567",
            Email = "info@beirutbasketball.lb",
            AreaId = hamra.Id,
            ManagerId = manager.Id,
            Status = StadiumStatus.Active,
            Latitude = 33.8938,
            Longitude = 35.5018
        };

        var stadium2 = new Stadium
        {
            Name = "Jounieh Sports Complex",
            Description = "Outdoor and indoor courts with stunning bay views. Free parking and a cafeteria on site.",
            Address = "Kaslik Road, Jounieh",
            Phone = "+961 9 876543",
            Email = "info@jouniehsports.lb",
            AreaId = jounieh.Id,
            ManagerId = manager.Id,
            Status = StadiumStatus.Active,
            Latitude = 33.9811,
            Longitude = 35.6186
        };

        var stadium3 = new Stadium
        {
            Name = "Achrafieh Court Club",
            Description = "Compact indoor club court ideal for 3-on-3 and training sessions, minutes from Sassine Square.",
            Address = "Sassine Square, Achrafieh, Beirut",
            Phone = "+961 1 445566",
            Email = "play@achrafiehcourt.lb",
            AreaId = achrafieh.Id,
            ManagerId = manager.Id,
            Status = StadiumStatus.Active,
            Latitude = 33.8869,
            Longitude = 35.5219
        };

        var stadium4 = new Stadium
        {
            Name = "Tripoli Seaside Courts",
            Description = "Two full outdoor courts by the sea in El Mina, popular for evening pickup games.",
            Address = "Corniche El Mina, Tripoli",
            Phone = "+961 6 223344",
            Email = "contact@tripolicourts.lb",
            AreaId = mina.Id,
            ManagerId = manager.Id,
            Status = StadiumStatus.Pending,
            Latitude = 34.4381,
            Longitude = 35.8308
        };

        context.Stadiums.AddRange(stadium1, stadium2, stadium3, stadium4);
        await context.SaveChangesAsync();

        context.Courts.AddRange(
            new Court { Name = "Main Court",     Description = "Full-size professional court with wood flooring", IsIndoor = true,  Capacity = 50, HourlyPrice = 80, Status = CourtStatus.Active, StadiumId = stadium1.Id },
            new Court { Name = "Training Court", Description = "Half-court for practice and small games",          IsIndoor = true,  Capacity = 20, HourlyPrice = 40, Status = CourtStatus.Active, StadiumId = stadium1.Id },
            new Court { Name = "Outdoor Court",  Description = "Full outdoor court with natural lighting",         IsIndoor = false, Capacity = 30, HourlyPrice = 50, Status = CourtStatus.Active, StadiumId = stadium2.Id },
            new Court { Name = "Bay Court",      Description = "Covered court overlooking Jounieh bay",            IsIndoor = true,  Capacity = 40, HourlyPrice = 65, Status = CourtStatus.Active, StadiumId = stadium2.Id },
            new Court { Name = "Club Court",     Description = "Indoor club court suited to 3-on-3",               IsIndoor = true,  Capacity = 24, HourlyPrice = 45, Status = CourtStatus.Active, StadiumId = stadium3.Id },
            new Court { Name = "Seaside Court A",Description = "Full outdoor court next to the corniche",          IsIndoor = false, Capacity = 30, HourlyPrice = 35, Status = CourtStatus.Active, StadiumId = stadium4.Id });

        foreach (var stadium in new[] { stadium1, stadium2, stadium3 })
        {
            for (int day = 1; day <= 6; day++)
            {
                context.OpeningHours.Add(new OpeningHour
                {
                    StadiumId = stadium.Id,
                    DayOfWeek = (DayOfWeek)day,
                    OpenTime = new TimeOnly(8, 0),
                    CloseTime = new TimeOnly(22, 0),
                    IsClosed = false
                });
            }

            context.OpeningHours.Add(new OpeningHour
            {
                StadiumId = stadium.Id,
                DayOfWeek = DayOfWeek.Sunday,
                OpenTime = new TimeOnly(10, 0),
                CloseTime = new TimeOnly(20, 0),
                IsClosed = false
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTimeSlotsAsync(ApplicationDbContext context)
    {
        if (await context.TimeSlots.AnyAsync()) return;

        var courts = await context.Courts.ToListAsync();
        var slots = new List<TimeSlot>();

        // Two weeks of hourly slots, 9:00 to 21:00, for every court.
        var today = DateTime.Today;
        for (int dayOffset = 0; dayOffset < 14; dayOffset++)
        {
            var date = DateOnly.FromDateTime(today.AddDays(dayOffset));

            foreach (var court in courts)
            {
                for (int hour = 9; hour < 21; hour++)
                {
                    slots.Add(new TimeSlot
                    {
                        CourtId = court.Id,
                        Date = date,
                        StartTime = new TimeOnly(hour, 0),
                        EndTime = new TimeOnly(hour + 1, 0),
                        IsAvailable = true
                    });
                }
            }
        }

        context.TimeSlots.AddRange(slots);
        await context.SaveChangesAsync();
    }
}
