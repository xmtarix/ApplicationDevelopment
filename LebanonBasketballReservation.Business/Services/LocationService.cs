using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Business.Services;

public class LocationService : ILocationService
{
    private readonly IUnitOfWork _uow;

    public LocationService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<GovernorateDto>> GetGovernoratesAsync(CancellationToken cancellationToken = default)
        => await _uow.Governorates.Query()
            .OrderBy(g => g.Name)
            .Select(g => new GovernorateDto(g.Id, g.Name, g.NameAr, g.Districts.Count))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DistrictDto>> GetDistrictsByGovernorateAsync(int governorateId, CancellationToken cancellationToken = default)
        => await _uow.Districts.Query()
            .Where(d => d.GovernorateId == governorateId)
            .OrderBy(d => d.Name)
            .Select(d => new DistrictDto(d.Id, d.Name, d.NameAr, d.GovernorateId, d.Governorate.Name, d.Areas.Count))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<AreaDto>> GetAreasByDistrictAsync(int districtId, CancellationToken cancellationToken = default)
        => await _uow.Areas.Query()
            .Where(a => a.DistrictId == districtId)
            .OrderBy(a => a.Name)
            .Select(a => new AreaDto(a.Id, a.Name, a.NameAr, a.DistrictId, a.District.Name, a.District.Governorate.Name, a.Stadiums.Count))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<GovernorateTreeDto>> GetHierarchyAsync(CancellationToken cancellationToken = default)
        => await _uow.Governorates.Query()
            .OrderBy(g => g.Name)
            .Select(g => new GovernorateTreeDto
            {
                Id = g.Id,
                Name = g.Name,
                NameAr = g.NameAr,
                Districts = g.Districts.OrderBy(d => d.Name).Select(d => new DistrictTreeDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    NameAr = d.NameAr,
                    GovernorateId = d.GovernorateId,
                    Areas = d.Areas.OrderBy(a => a.Name).Select(a => new AreaTreeDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        NameAr = a.NameAr,
                        DistrictId = a.DistrictId,
                        StadiumCount = a.Stadiums.Count
                    }).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<GovernorateDto> CreateGovernorateAsync(string name, string nameAr, CancellationToken cancellationToken = default)
    {
        name = Require(name, "Governorate name");
        nameAr = nameAr?.Trim() ?? "";

        if (await _uow.Governorates.ExistsAsync(g => g.Name == name, cancellationToken))
            throw new ConflictException($"A governorate named \"{name}\" already exists.");

        var g = new Governorate { Name = name, NameAr = nameAr };
        await _uow.Governorates.AddAsync(g, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new GovernorateDto(g.Id, g.Name, g.NameAr);
    }

    public async Task<DistrictDto> CreateDistrictAsync(string name, string nameAr, int governorateId, CancellationToken cancellationToken = default)
    {
        name = Require(name, "District name");
        nameAr = nameAr?.Trim() ?? "";

        var governorate = await _uow.Governorates.GetByIdAsync(governorateId, cancellationToken)
            ?? throw NotFoundException.For("Governorate", governorateId);

        if (await _uow.Districts.ExistsAsync(d => d.GovernorateId == governorateId && d.Name == name, cancellationToken))
            throw new ConflictException($"\"{governorate.Name}\" already has a district named \"{name}\".");

        var d = new District { Name = name, NameAr = nameAr, GovernorateId = governorateId };
        await _uow.Districts.AddAsync(d, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new DistrictDto(d.Id, d.Name, d.NameAr, d.GovernorateId, governorate.Name);
    }

    public async Task<AreaDto> CreateAreaAsync(string name, string nameAr, int districtId, CancellationToken cancellationToken = default)
    {
        name = Require(name, "Area name");
        nameAr = nameAr?.Trim() ?? "";

        var district = await _uow.Districts.Find(d => d.Id == districtId)
            .Include(d => d.Governorate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("District", districtId);

        if (await _uow.Areas.ExistsAsync(a => a.DistrictId == districtId && a.Name == name, cancellationToken))
            throw new ConflictException($"\"{district.Name}\" already has an area named \"{name}\".");

        var a = new Area { Name = name, NameAr = nameAr, DistrictId = districtId };
        await _uow.Areas.AddAsync(a, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AreaDto(a.Id, a.Name, a.NameAr, a.DistrictId, district.Name, district.Governorate.Name);
    }

    public async Task DeleteGovernorateAsync(int id, CancellationToken cancellationToken = default)
    {
        var g = await _uow.Governorates.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Governorate", id);

        // Refuse rather than cascade: deleting a populated governorate would silently
        // take its districts, areas and stadiums with it.
        if (await _uow.Districts.ExistsAsync(d => d.GovernorateId == id, cancellationToken))
            throw new ConflictException("Remove this governorate's districts before deleting it.");

        _uow.Governorates.Delete(g);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDistrictAsync(int id, CancellationToken cancellationToken = default)
    {
        var d = await _uow.Districts.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("District", id);

        if (await _uow.Areas.ExistsAsync(a => a.DistrictId == id, cancellationToken))
            throw new ConflictException("Remove this district's areas before deleting it.");

        _uow.Districts.Delete(d);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _uow.Areas.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Area", id);

        if (await _uow.Stadiums.ExistsAsync(s => s.AreaId == id, cancellationToken))
            throw new ConflictException("Stadiums are registered in this area, so it cannot be deleted.");

        _uow.Areas.Delete(a);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static string Require(string? value, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ValidationException($"{fieldName} is required.");
        return trimmed;
    }
}
