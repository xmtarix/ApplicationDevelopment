using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.UnitOfWork;
using LebanonBasketballReservation.Web.ViewModels.Stadium;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LebanonBasketballReservation.Web.Areas.Manager.Controllers;

public class StadiumController : ManagerBaseController
{
    private readonly IStadiumService _stadiums;
    private readonly ILocationService _locations;
    private readonly IFileService _files;
    private readonly IUnitOfWork _uow;

    public StadiumController(
        IStadiumService stadiums,
        ILocationService locations,
        IFileService files,
        IUnitOfWork uow,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _stadiums = stadiums;
        _locations = locations;
        _files = files;
        _uow = uow;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _stadiums.GetByManagerAsync(ManagerId, cancellationToken));

    /// <summary>Refills the cascading location dropdowns for whatever is currently selected.</summary>
    private async Task PopulateLocationsAsync(CreateStadiumViewModel vm, CancellationToken cancellationToken)
    {
        vm.Governorates = await _locations.GetGovernoratesAsync(cancellationToken);

        if (vm.GovernorateId > 0)
            vm.Districts = await _locations.GetDistrictsByGovernorateAsync(vm.GovernorateId, cancellationToken);

        if (vm.DistrictId > 0)
            vm.Areas = await _locations.GetAreasByDistrictAsync(vm.DistrictId, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = new CreateStadiumViewModel();
        await PopulateLocationsAsync(vm, cancellationToken);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStadiumViewModel vm, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        ValidateImage(imageFile);

        if (!ModelState.IsValid)
        {
            await PopulateLocationsAsync(vm, cancellationToken);
            return View(vm);
        }

        try
        {
            var stadium = await _stadiums.CreateAsync(new CreateStadiumDto
            {
                Name = vm.Name,
                Description = vm.Description,
                Address = vm.Address,
                Phone = vm.Phone,
                Email = vm.Email,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                AreaId = vm.AreaId
            }, ManagerId, cancellationToken);

            await SaveMainImageAsync(stadium.Id, imageFile, cancellationToken);

            Success("Stadium submitted. An administrator will review it shortly.");
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLocationsAsync(vm, cancellationToken);
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Ownership is enforced by id inside the service, not by comparing display names.
            var s = await _stadiums.GetOwnedByIdAsync(id, ManagerId, cancellationToken);

            var vm = new CreateStadiumViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Address = s.Address,
                Phone = s.Phone,
                Email = s.Email,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                // Carrying the full location chain is what keeps AreaId from resetting on save.
                GovernorateId = s.GovernorateId,
                DistrictId = s.DistrictId,
                AreaId = s.AreaId,
                CurrentImagePath = s.MainImagePath
            };

            await PopulateLocationsAsync(vm, cancellationToken);
            return View(vm);
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ForbiddenException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CreateStadiumViewModel vm, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        ValidateImage(imageFile);

        if (!ModelState.IsValid)
        {
            await PopulateLocationsAsync(vm, cancellationToken);
            return View(vm);
        }

        try
        {
            await _stadiums.UpdateAsync(new UpdateStadiumDto
            {
                Id = vm.Id,
                Name = vm.Name,
                Description = vm.Description,
                Address = vm.Address,
                Phone = vm.Phone,
                Email = vm.Email,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                AreaId = vm.AreaId
            }, ManagerId, cancellationToken);

            await SaveMainImageAsync(vm.Id, imageFile, cancellationToken);

            Success("Stadium updated.");
            return RedirectToAction(nameof(Index));
        }
        catch (ForbiddenException) { return Forbid(); }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLocationsAsync(vm, cancellationToken);
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _stadiums.DeleteAsync(id, ManagerId, cancellationToken);
            Success("Stadium deleted.");
        }
        catch (DomainException ex) { Error(ex.Message); }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Adds an image-specific model error so the message lands next to the field.</summary>
    private void ValidateImage(IFormFile? imageFile)
    {
        if (imageFile is not { Length: > 0 }) return;

        var error = _files.GetValidationError(imageFile.FileName, imageFile.Length);
        if (error is not null) ModelState.AddModelError("imageFile", error);
    }

    /// <summary>Stores the upload as the stadium's main image, replacing any previous one.</summary>
    private async Task SaveMainImageAsync(int stadiumId, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile is not { Length: > 0 }) return;
        if (!_files.IsValidImage(imageFile.FileName, imageFile.Length)) return;

        await using var stream = imageFile.OpenReadStream();
        var path = await _files.SaveImageAsync(stream, imageFile.FileName, "stadiums", cancellationToken);

        var existing = await _uow.StadiumImages
            .Find(i => i.StadiumId == stadiumId && i.IsMain)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var oldPath = existing.ImagePath;
            existing.ImagePath = path;
            _uow.StadiumImages.Update(existing);
            await _uow.SaveChangesAsync(cancellationToken);

            // Remove the old file only after the new path is safely persisted.
            _files.DeleteImage(oldPath);
        }
        else
        {
            await _uow.StadiumImages.AddAsync(new StadiumImage
            {
                StadiumId = stadiumId,
                ImagePath = path,
                IsMain = true,
                DisplayOrder = 1
            }, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
