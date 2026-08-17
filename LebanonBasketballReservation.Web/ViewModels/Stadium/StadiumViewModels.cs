using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LebanonBasketballReservation.Web.ViewModels.Stadium;

public class StadiumSearchViewModel
{
    [Display(Name = "Search")] public string? Name { get; set; }
    public int? GovernorateId { get; set; }
    public int? DistrictId { get; set; }
    public int? AreaId { get; set; }
    public bool? IsIndoor { get; set; }

    [Display(Name = "Min price")] public decimal? MinPrice { get; set; }
    [Display(Name = "Max price")] public decimal? MaxPrice { get; set; }

    public StadiumSortOrder SortBy { get; set; } = StadiumSortOrder.Newest;
    public int Page { get; set; } = 1;

    public PagedResult<StadiumDto>? Results { get; set; }
    public IEnumerable<GovernorateDto> Governorates { get; set; } = Enumerable.Empty<GovernorateDto>();
    public IEnumerable<DistrictDto> Districts { get; set; } = Enumerable.Empty<DistrictDto>();
    public IEnumerable<AreaDto> Areas { get; set; } = Enumerable.Empty<AreaDto>();
    public HashSet<int> FavoriteIds { get; set; } = new();

    /// <summary>True when any filter is active, so the UI can offer a "clear" affordance.</summary>
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Name) || GovernorateId.HasValue || DistrictId.HasValue
        || AreaId.HasValue || IsIndoor.HasValue || MinPrice.HasValue || MaxPrice.HasValue;

    /// <summary>Current filters as route values, for paging and sort links.</summary>
    public Dictionary<string, string?> RouteValues() => new()
    {
        ["Name"] = Name,
        ["GovernorateId"] = GovernorateId?.ToString(),
        ["DistrictId"] = DistrictId?.ToString(),
        ["AreaId"] = AreaId?.ToString(),
        ["IsIndoor"] = IsIndoor?.ToString(),
        ["MinPrice"] = MinPrice?.ToString(),
        ["MaxPrice"] = MaxPrice?.ToString(),
        ["SortBy"] = SortBy.ToString()
    };
}

public class StadiumDetailsViewModel
{
    public StadiumDto Stadium { get; set; } = null!;
    public bool IsFavorite { get; set; }
}

/// <summary>Everything the shared stadium card partial needs to render one tile.</summary>
public class StadiumCardModel
{
    public StadiumDto Stadium { get; set; } = null!;
    public bool IsFavorite { get; set; }

    /// <summary>When true, renders the favorite heart (customers only).</summary>
    public bool ShowFavorite { get; set; }

    /// <summary>Where the favorite toggle should send the user back to.</summary>
    public string? ReturnUrl { get; set; }
}

public class CreateStadiumViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Stadium name is required.")]
    [MaxLength(150), Display(Name = "Stadium name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the stadium.")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number."), MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address."), MaxLength(100)]
    public string? Email { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Please select a governorate.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a governorate.")]
    [Display(Name = "Governorate")]
    public int GovernorateId { get; set; }

    [Required(ErrorMessage = "Please select a district.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a district.")]
    [Display(Name = "District")]
    public int DistrictId { get; set; }

    [Required(ErrorMessage = "Please select an area.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select an area.")]
    [Display(Name = "Area")]
    public int AreaId { get; set; }

    /// <summary>Existing main image, shown when editing.</summary>
    public string? CurrentImagePath { get; set; }

    public IEnumerable<GovernorateDto> Governorates { get; set; } = Enumerable.Empty<GovernorateDto>();
    public IEnumerable<DistrictDto> Districts { get; set; } = Enumerable.Empty<DistrictDto>();
    public IEnumerable<AreaDto> Areas { get; set; } = Enumerable.Empty<AreaDto>();

    public bool IsEdit => Id > 0;
}
