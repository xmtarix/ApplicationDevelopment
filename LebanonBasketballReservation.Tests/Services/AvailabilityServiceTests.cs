using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using LebanonBasketballReservation.Data.UnitOfWork;
using Moq;

namespace LebanonBasketballReservation.Tests.Services;

public class AvailabilityServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITimeSlotRepository> _slotRepo = new();
    private readonly TestClock _clock = new();
    private readonly AvailabilityService _service;

    public AvailabilityServiceTests()
    {
        _uow.Setup(u => u.TimeSlots).Returns(_slotRepo.Object);
        _service = new AvailabilityService(_uow.Object, _clock);
    }

    /// <summary>A slot starting the given number of hours from the test clock's "now".</summary>
    private TimeSlot SlotStartingIn(int id, double hours)
    {
        var start = _clock.LocalNow.AddHours(hours);
        return new TimeSlot
        {
            Id = id,
            CourtId = 1,
            Date = DateOnly.FromDateTime(start),
            StartTime = TimeOnly.FromDateTime(start),
            EndTime = TimeOnly.FromDateTime(start.AddHours(1)),
            IsAvailable = true
        };
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsFutureSlots()
    {
        var date = _clock.Today;
        _slotRepo.Setup(r => r.GetAvailableAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot> { SlotStartingIn(1, 2), SlotStartingIn(2, 4) });

        var result = await _service.GetAvailableSlotsAsync(1, date);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesSlotsThatAlreadyStarted()
    {
        var date = _clock.Today;
        _slotRepo.Setup(r => r.GetAvailableAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>
            {
                SlotStartingIn(1, -3), // already started — must not be offered
                SlotStartingIn(2, 3)
            });

        var result = await _service.GetAvailableSlotsAsync(1, date);

        Assert.Single(result);
        Assert.Equal(2, result.First().Id);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenNoSlots_ReturnsEmpty()
    {
        var date = _clock.Today;
        _slotRepo.Setup(r => r.GetAvailableAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>());

        var result = await _service.GetAvailableSlotsAsync(1, date);

        Assert.Empty(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotNotFound_ReturnsFalse()
    {
        _slotRepo.Setup(r => r.GetWithDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSlot?)null);

        Assert.False(await _service.IsSlotAvailableAsync(99));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenFreeAndInFuture_ReturnsTrue()
    {
        _slotRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SlotStartingIn(1, 5));

        Assert.True(await _service.IsSlotAvailableAsync(1));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotAlreadyStarted_ReturnsFalse()
    {
        _slotRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SlotStartingIn(1, -1));

        Assert.False(await _service.IsSlotAvailableAsync(1));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenActiveReservationExists_ReturnsFalse()
    {
        var slot = SlotStartingIn(1, 5);
        slot.Reservation = new Reservation { Id = 1, Status = ReservationStatus.Confirmed };

        _slotRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        Assert.False(await _service.IsSlotAvailableAsync(1));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenPreviousReservationCancelled_ReturnsTrue()
    {
        // A cancelled booking releases the slot, so it must become bookable again.
        var slot = SlotStartingIn(1, 5);
        slot.Reservation = new Reservation { Id = 1, Status = ReservationStatus.Cancelled };

        _slotRepo.Setup(r => r.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        Assert.True(await _service.IsSlotAvailableAsync(1));
    }

    [Fact]
    public async Task GetAvailabilityCalendarAsync_GroupsSlotsByDate()
    {
        _slotRepo.Setup(r => r.GetAvailableFromAsync(1, It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>
            {
                SlotStartingIn(1, 2),
                SlotStartingIn(2, 3),
                SlotStartingIn(3, 26) // next day
            });

        var calendar = (await _service.GetAvailabilityCalendarAsync(1, _clock.Today)).ToList();

        Assert.Equal(2, calendar.Count);
        Assert.Equal(2, calendar[0].Count);
        Assert.Equal(1, calendar[1].Count);
    }
}
