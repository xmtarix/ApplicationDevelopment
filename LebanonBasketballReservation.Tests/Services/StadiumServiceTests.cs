using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LebanonBasketballReservation.Tests.Services;

public class StadiumServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStadiumRepository> _stadiumRepo = new();
    private readonly Mock<IRepository<Area>> _areaRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly StadiumService _service;

    public StadiumServiceTests()
    {
        _uow.Setup(u => u.Stadiums).Returns(_stadiumRepo.Object);
        _uow.Setup(u => u.Areas).Returns(_areaRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _stadiumRepo
            .Setup(r => r.GetRatingsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, (double, int)>());

        _service = new StadiumService(
            _uow.Object,
            _notifications.Object,
            new TestClock(),
            NullLogger<StadiumService>.Instance);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsForbidden()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, ManagerId = "other-manager", Name = "Test" });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateAsync(new UpdateStadiumDto { Id = 1, Name = "New Name", AreaId = 1 }, "wrong-manager"));
    }

    [Fact]
    public async Task UpdateAsync_WhenAreaMissing_ThrowsValidation()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, ManagerId = "manager-1" });

        _areaRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Area, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<LebanonBasketballReservation.Business.Exceptions.ValidationException>(
            () => _service.UpdateAsync(new UpdateStadiumDto { Id = 1, Name = "Name", AreaId = 999 }, "manager-1"));
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsForbidden()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, ManagerId = "other-manager" });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteAsync(1, "wrong-manager"));
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _stadiumRepo.Setup(r => r.GetWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stadium?)null);

        Assert.Null(await _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetOwnedByIdAsync_WhenNotOwner_ThrowsForbidden()
    {
        _stadiumRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, ManagerId = "owner", Name = "Test" });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.GetOwnedByIdAsync(1, "intruder"));
    }

    [Fact]
    public async Task GetOwnedByIdAsync_WhenOwner_ReturnsStadium()
    {
        _stadiumRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, ManagerId = "owner", Name = "Test", AreaId = 5 });

        var result = await _service.GetOwnedByIdAsync(1, "owner");

        Assert.Equal(1, result.Id);
        // AreaId must survive the round trip, or editing would silently reset the location.
        Assert.Equal(5, result.AreaId);
    }

    [Fact]
    public async Task ApproveAsync_SetsStatusToActiveAndNotifies()
    {
        var stadium = new Stadium { Id = 1, Status = StadiumStatus.Pending, Name = "Test", ManagerId = "m1" };
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(stadium);

        await _service.ApproveAsync(1);

        Assert.Equal(StadiumStatus.Active, stadium.Status);
        _notifications.Verify(n => n.CreateAsync(
            "m1", It.IsAny<string>(), It.IsAny<string>(),
            NotificationType.StadiumApproved, It.IsAny<int?>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyActive_ThrowsConflict()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, Status = StadiumStatus.Active, ManagerId = "m1" });

        await Assert.ThrowsAsync<ConflictException>(() => _service.ApproveAsync(1));
    }

    [Fact]
    public async Task ApproveAsync_WhenNotFound_ThrowsNotFound()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stadium?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveAsync(99));
    }

    [Fact]
    public async Task RejectAsync_SetsStatusToRejected()
    {
        var stadium = new Stadium { Id = 1, Status = StadiumStatus.Pending, Name = "Test", ManagerId = "m1" };
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(stadium);

        await _service.RejectAsync(1, "Incomplete address");

        Assert.Equal(StadiumStatus.Rejected, stadium.Status);
    }

    [Fact]
    public async Task SetActiveAsync_WhenStadiumPending_ThrowsConflict()
    {
        _stadiumRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stadium { Id = 1, Status = StadiumStatus.Pending, ManagerId = "m1" });

        await Assert.ThrowsAsync<ConflictException>(() => _service.SetActiveAsync(1, false));
    }
}
