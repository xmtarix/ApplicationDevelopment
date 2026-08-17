using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LebanonBasketballReservation.Business.DTOs;

public class StadiumDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public StadiumStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public int AreaId { get; set; }
    public int DistrictId { get; set; }
    public int GovernorateId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string GovernorateName { get; set; } = string.Empty;

    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string? ManagerEmail { get; set; }

    public int CourtCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? MainImagePath { get; set; }

    public List<string> ImagePaths { get; set; } = new();
    public List<CourtDto> Courts { get; set; } = new();
    public List<ReviewDto> Reviews { get; set; } = new();
    public List<OpeningHourDto> OpeningHours { get; set; } = new();

    /// <summary>Cheapest active court, used for the "from $X/hr" label.</summary>
    public decimal? MinPrice => Courts.Where(c => c.IsBookable).Select(c => (decimal?)c.HourlyPrice).Min();

    public string FullLocation => string.Join(", ",
        new[] { AreaName, DistrictName, GovernorateName }.Where(p => !string.IsNullOrWhiteSpace(p)));

    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
}

public class ReviewDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int StadiumId { get; set; }
}

public class OpeningHourDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public string DayName => DayOfWeek.ToString();
    public string Hours => IsClosed ? "Closed" : $"{OpenTime:HH\\:mm} – {CloseTime:HH\\:mm}";
}

public class CreateStadiumDto
{
    [Required(ErrorMessage = "Stadium name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the stadium.")]
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20), Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    [MaxLength(100), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string? Email { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Please select an area.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select an area.")]
    public int AreaId { get; set; }
}

public class UpdateStadiumDto : CreateStadiumDto
{
    public int Id { get; set; }
}

public class StadiumSearchDto
{
    public string? Name { get; set; }
    public int? GovernorateId { get; set; }
    public int? DistrictId { get; set; }
    public int? AreaId { get; set; }
    public bool? IsIndoor { get; set; }

    [Range(0, 10000)] public decimal? MinPrice { get; set; }
    [Range(0, 10000)] public decimal? MaxPrice { get; set; }

    public StadiumSortOrder SortBy { get; set; } = StadiumSortOrder.Newest;

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = 12;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 48 ? 12 : value;
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);
}
