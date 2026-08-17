namespace LebanonBasketballReservation.Business.DTOs;

public record GovernorateDto(int Id, string Name, string NameAr, int DistrictCount = 0);

public record DistrictDto(int Id, string Name, string NameAr, int GovernorateId, string GovernorateName, int AreaCount = 0);

public record AreaDto(int Id, string Name, string NameAr, int DistrictId, string DistrictName, string GovernorateName, int StadiumCount = 0);

/// <summary>Governorate with its districts and areas, for the admin locations tree.</summary>
public class GovernorateTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public List<DistrictTreeDto> Districts { get; set; } = new();
    public int AreaCount => Districts.Sum(d => d.Areas.Count);
    public int StadiumCount => Districts.Sum(d => d.StadiumCount);
}

public class DistrictTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int GovernorateId { get; set; }
    public List<AreaTreeDto> Areas { get; set; } = new();
    public int StadiumCount => Areas.Sum(a => a.StadiumCount);
}

public class AreaTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public int StadiumCount { get; set; }
}
