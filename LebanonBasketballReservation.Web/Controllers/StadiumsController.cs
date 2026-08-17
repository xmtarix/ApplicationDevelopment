using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data;
using LebanonBasketballReservation.Web.ViewModels.Stadium;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LebanonBasketballReservation.Data.Entities;

namespace LebanonBasketballReservation.Web.Controllers;

public class StadiumsController : Controller
{
    private readonly IStadiumService _stadiums;
    private readonly ILocationService _locations;
    private readonly IAvailabilityService _availability;
    private readonly IFavoriteService _favorites;
    private readonly UserManager<ApplicationUser> _userManager;

    public StadiumsController(
        IStadiumService stadiums,
        ILocationService locations,
        IAvailabilityService availability,
        IFavoriteService favorites,
        UserManager<ApplicationUser> userManager)
    {
        _stadiums = stadiums;
        _locations = locations;
        _availability = availability;
        _favorites = favorites;
        _userManager = userManager;
    }

    /// <summary>Favorited ids for the signed-in customer, or empty for everyone else.</summary>
    private async Task<HashSet<int>> GetFavoriteIdsAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole(RoleNames.Customer))
            return [];

        var userId = _userManager.GetUserId(User);
        return string.IsNullOrEmpty(userId)
            ? []
            : await _favorites.GetFavoriteIdsAsync(userId, cancellationToken);
    }

    public async Task<IActionResult> Index(StadiumSearchViewModel vm, CancellationToken cancellationToken)
    {
        vm.Results = await _stadiums.SearchAsync(new StadiumSearchDto
        {
            Name = vm.Name,
            GovernorateId = vm.GovernorateId,
            DistrictId = vm.DistrictId,
            AreaId = vm.AreaId,
            IsIndoor = vm.IsIndoor,
            MinPrice = vm.MinPrice,
            MaxPrice = vm.MaxPrice,
            SortBy = vm.SortBy,
            Page = vm.Page
        }, cancellationToken);

        vm.Governorates = await _locations.GetGovernoratesAsync(cancellationToken);

        // Only load the dependent dropdowns that the current selection actually needs.
        if (vm.GovernorateId.HasValue)
            vm.Districts = await _locations.GetDistrictsByGovernorateAsync(vm.GovernorateId.Value, cancellationToken);
        if (vm.DistrictId.HasValue)
            vm.Areas = await _locations.GetAreasByDistrictAsync(vm.DistrictId.Value, cancellationToken);

        vm.FavoriteIds = await GetFavoriteIdsAsync(cancellationToken);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var stadium = await _stadiums.GetByIdAsync(id, cancellationToken);
        if (stadium is null) return NotFound();

        // Pending and rejected stadiums are not public; only their manager or an admin may look.
        if (stadium.Status != Data.Enums.StadiumStatus.Active)
        {
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isOwner = User.Identity?.IsAuthenticated == true
                       && _userManager.GetUserId(User) == stadium.ManagerId;

            if (!isAdmin && !isOwner) return NotFound();
        }

        var favorites = await GetFavoriteIdsAsync(cancellationToken);

        return View(new StadiumDetailsViewModel
        {
            Stadium = stadium,
            IsFavorite = favorites.Contains(id)
        });
    }

    /// <summary>JSON endpoint backing the date picker on the details page.</summary>
    [HttpGet]
    public async Task<IActionResult> Availability(int courtId, string? date, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest(new { error = "Provide a valid date in yyyy-MM-dd format." });

        var slots = await _availability.GetAvailableSlotsAsync(courtId, parsed, cancellationToken);

        return Json(slots.Select(s => new
        {
            id = s.Id,
            start = s.StartTime.ToString("HH\\:mm"),
            end = s.EndTime.ToString("HH\\:mm"),
            label = s.Label
        }));
    }
}
