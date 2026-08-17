using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LebanonBasketballReservation.Tests.Services;

public class ReservationServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IReservationRepository> _reservationRepo = new();
    private readonly Mock<ITimeSlotRepository> _slotRepo = new();
    private readonly TestClock _clock = new();
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _uow.Setup(u => u.Reservations).Returns(_reservationRepo.Object);
        _uow.Setup(u => u.TimeSlots).Returns(_slotRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Run the transaction body inline so tests exercise the real logic without a database.
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<int>>, CancellationToken>((action, _) => action());
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReservationSettings:CancellationWindowHours"] = "24"
            })
            .Build();

        _service = new ReservationService(
            _uow.Object,
            _notifications.Object,
            _clock,
            config,
            NullLogger<ReservationService>.Instance);
    }

    /// <summary>Builds a reservation whose slot starts a given number of hours from "now".</summary>
    private Reservation BuildReservation(
        string customerId = "user-1",
        ReservationStatus status = ReservationStatus.Confirmed,
        double startsInHours = 72)
    {
        var start = _clock.LocalNow.AddHours(startsInHours);

        var stadium = new Stadium { Id = 1, Name = "Test Arena", ManagerId = "manager-1" };
        var court = new Court { Id = 1, Name = "Main Court", HourlyPrice = 50, StadiumId = 1, Stadium = stadium };

        var slot = new TimeSlot
        {
            Id = 1,
            CourtId = 1,
            Court = court,
            Date = DateOnly.FromDateTime(start),
            StartTime = TimeOnly.FromDateTime(start),
            EndTime = TimeOnly.FromDateTime(start.AddHours(1)),
            IsAvailable = false
        };

        return new Reservation
        {
            Id = 1,
            CustomerId = customerId,
            Status = status,
            TimeSlot = slot,
            TimeSlotId = 1,
            TotalPrice = 50,
            Customer = new ApplicationUser { FirstName = "Test", LastName = "User" }
        };
    }

    [Fact]
    public async Task CancelAsync_WhenNotOwner_ThrowsForbidden()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(customerId: "other-user"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.CancelAsync(1, "different-user", null));
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyCancelled_ThrowsConflict()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(status: ReservationStatus.Cancelled));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CancelAsync(1, "user-1", null));
    }

    [Fact]
    public async Task CancelAsync_PastCancellationWindow_ThrowsConflict()
    {
        // Starts in one hour, but the window requires 24 hours' notice.
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(startsInHours: 1));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CancelAsync(1, "user-1", null));
    }

    [Fact]
    public async Task CancelAsync_WithinWindow_CancelsAndReleasesSlot()
    {
        var reservation = BuildReservation(startsInHours: 72);
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        await _service.CancelAsync(1, "user-1", "Change of plans");

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal("Change of plans", reservation.CancellationReason);
        Assert.NotNull(reservation.CancelledAt);

        // The slot must go back on sale, otherwise it is lost inventory.
        _slotRepo.Verify(s => s.ReleaseAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_PendingReservation_IsAlwaysAllowed()
    {
        // A pending request is not yet committed, so the window does not apply.
        var reservation = BuildReservation(status: ReservationStatus.Pending, startsInHours: 1);
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        await _service.CancelAsync(1, "user-1", null);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotOwner_ReturnsNull()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(customerId: "owner"));

        var result = await _service.GetByIdAsync(1, "someone-else");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwner_ReturnsReservation()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(customerId: "user-1"));

        var result = await _service.GetByIdAsync(1, "user-1");

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenStadiumManager_ReturnsReservation()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(customerId: "user-1"));

        var result = await _service.GetByIdAsync(1, "manager-1");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByCustomerAsync_ReturnsCustomerReservations()
    {
        _reservationRepo.Setup(r => r.GetByCustomerAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { BuildReservation() });

        var result = await _service.GetByCustomerAsync("user-1");

        Assert.Single(result);
    }

    [Fact]
    public async Task ConfirmAsync_WhenNotStadiumManager_ThrowsForbidden()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(status: ReservationStatus.Pending));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.ConfirmAsync(1, "not-the-manager"));
    }

    [Fact]
    public async Task ConfirmAsync_WhenPending_SetsConfirmed()
    {
        var reservation = BuildReservation(status: ReservationStatus.Pending);
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        await _service.ConfirmAsync(1, "manager-1");

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenNotPending_ThrowsConflict()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(status: ReservationStatus.Confirmed));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.ConfirmAsync(1, "manager-1"));
    }

    [Fact]
    public async Task RejectAsync_ReleasesSlotAndSetsRejected()
    {
        var reservation = BuildReservation(status: ReservationStatus.Pending);
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        await _service.RejectAsync(1, "manager-1", "Court under maintenance");

        Assert.Equal(ReservationStatus.Rejected, reservation.Status);
        Assert.Equal("Court under maintenance", reservation.CancellationReason);
        _slotRepo.Verify(s => s.ReleaseAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenNotCompleted_ThrowsConflict()
    {
        _reservationRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(status: ReservationStatus.Confirmed));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.SubmitReviewAsync(1, "user-1", 5, "Great court"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task SubmitReviewAsync_WithRatingOutOfRange_ThrowsValidation(int rating)
    {
        await Assert.ThrowsAsync<LebanonBasketballReservation.Business.Exceptions.ValidationException>(
            () => _service.SubmitReviewAsync(1, "user-1", rating, null));
    }

    [Fact]
    public async Task CreateAsync_WhenSlotAlreadyClaimed_ThrowsConflict()
    {
        var reservation = BuildReservation(status: ReservationStatus.Pending, startsInHours: 48);
        var slot = reservation.TimeSlot;
        slot.IsAvailable = true;
        slot.Reservation = null;
        slot.Court!.Stadium!.Status = StadiumStatus.Active;
        slot.Court.Status = CourtStatus.Active;

        _slotRepo.Setup(s => s.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        // Another request won the race for this slot.
        _slotRepo.Setup(s => s.TryClaimAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new Business.DTOs.CreateReservationDto { TimeSlotId = 1 }, "user-1"));
    }

    [Fact]
    public async Task CreateAsync_WhenSlotAlreadyStarted_ThrowsConflict()
    {
        var reservation = BuildReservation(startsInHours: -2);
        var slot = reservation.TimeSlot;
        slot.IsAvailable = true;
        slot.Reservation = null;
        slot.Court!.Stadium!.Status = StadiumStatus.Active;
        slot.Court.Status = CourtStatus.Active;

        _slotRepo.Setup(s => s.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new Business.DTOs.CreateReservationDto { TimeSlotId = 1 }, "user-1"));
    }

    [Fact]
    public async Task CreateAsync_WhenStadiumNotActive_ThrowsConflict()
    {
        var reservation = BuildReservation(startsInHours: 48);
        var slot = reservation.TimeSlot;
        slot.IsAvailable = true;
        slot.Reservation = null;
        slot.Court!.Stadium!.Status = StadiumStatus.Pending;

        _slotRepo.Setup(s => s.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new Business.DTOs.CreateReservationDto { TimeSlotId = 1 }, "user-1"));
    }

    [Fact]
    public async Task CreateAsync_WhenSlotMissing_ThrowsNotFound()
    {
        _slotRepo.Setup(s => s.GetWithDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSlot?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(new Business.DTOs.CreateReservationDto { TimeSlotId = 99 }, "user-1"));
    }
}
