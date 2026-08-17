using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Services;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Repositories.Interfaces;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LebanonBasketballReservation.Tests.Services;

public class CourtServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICourtRepository> _courtRepo = new();
    private readonly Mock<ITimeSlotRepository> _slotRepo = new();
    private readonly Mock<IStadiumRepository> _stadiumRepo = new();
    private readonly TestClock _clock = new();
    private readonly CourtService _service;

    private const string ManagerId = "manager-1";

    public CourtServiceTests()
    {
        _uow.Setup(u => u.Courts).Returns(_courtRepo.Object);
        _uow.Setup(u => u.TimeSlots).Returns(_slotRepo.Object);
        _uow.Setup(u => u.Stadiums).Returns(_stadiumRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _courtRepo.Setup(r => r.GetWithStadiumAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Court
            {
                Id = 1,
                Name = "Main Court",
                StadiumId = 1,
                Stadium = new Stadium { Id = 1, ManagerId = ManagerId, Name = "Arena" }
            });

        _slotRepo.Setup(r => r.GetExistingKeysAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(DateOnly, TimeOnly)>());

        _service = new CourtService(_uow.Object, _clock, NullLogger<CourtService>.Instance);
    }

    private GenerateTimeSlotsDto BuildRequest(int days = 1, int durationMinutes = 60)
        => new()
        {
            CourtId = 1,
            FromDate = _clock.Today,
            ToDate = _clock.Today.AddDays(days - 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = durationMinutes
        };

    [Fact]
    public async Task GenerateTimeSlotsAsync_CreatesOneSlotPerInterval()
    {
        List<TimeSlot>? captured = null;
        _slotRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TimeSlot>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TimeSlot>, CancellationToken>((slots, _) => captured = slots.ToList())
            .Returns(Task.CompletedTask);

        // 09:00–12:00 in 60-minute slots is three slots on a single day.
        var created = await _service.GenerateTimeSlotsAsync(BuildRequest(), ManagerId);

        Assert.Equal(3, created);
        Assert.NotNull(captured);
        Assert.Equal(new TimeOnly(9, 0), captured![0].StartTime);
        Assert.Equal(new TimeOnly(11, 0), captured[2].StartTime);
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_SkipsSlotsThatAlreadyExist()
    {
        _slotRepo.Setup(r => r.GetExistingKeysAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(DateOnly, TimeOnly)>
            {
                (_clock.Today, new TimeOnly(9, 0))
            });

        var created = await _service.GenerateTimeSlotsAsync(BuildRequest(), ManagerId);

        Assert.Equal(2, created);
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_SpansMultipleDays()
    {
        var created = await _service.GenerateTimeSlotsAsync(BuildRequest(days: 3), ManagerId);

        Assert.Equal(9, created); // 3 slots × 3 days
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_HonoursExcludedDays()
    {
        var request = BuildRequest(days: 7);
        request.ExcludedDays = [_clock.Today.DayOfWeek];

        var created = await _service.GenerateTimeSlotsAsync(request, ManagerId);

        Assert.Equal(18, created); // 6 days × 3 slots
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_WhenDateRangeInverted_ThrowsValidation()
    {
        var request = BuildRequest();
        request.ToDate = request.FromDate.AddDays(-1);

        await Assert.ThrowsAsync<LebanonBasketballReservation.Business.Exceptions.ValidationException>(
            () => _service.GenerateTimeSlotsAsync(request, ManagerId));
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_WhenClosingBeforeOpening_ThrowsValidation()
    {
        var request = BuildRequest();
        request.EndTime = new TimeOnly(8, 0);

        await Assert.ThrowsAsync<LebanonBasketballReservation.Business.Exceptions.ValidationException>(
            () => _service.GenerateTimeSlotsAsync(request, ManagerId));
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_WhenNotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.GenerateTimeSlotsAsync(BuildRequest(), "someone-else"));
    }

    [Fact]
    public async Task GenerateTimeSlotsAsync_WhenCourtMissing_ThrowsNotFound()
    {
        _courtRepo.Setup(r => r.GetWithStadiumAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Court?)null);

        var request = BuildRequest();
        request.CourtId = 99;

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GenerateTimeSlotsAsync(request, ManagerId));
    }

    [Fact]
    public async Task CreateAsync_WhenNameDuplicated_ThrowsConflict()
    {
        _stadiumRepo.Setup(r => r.IsOwnedByAsync(1, ManagerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courtRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Court, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new CreateCourtDto { Name = "Main Court", StadiumId = 1 }, ManagerId));
    }

    [Fact]
    public async Task CreateAsync_WhenNotStadiumOwner_ThrowsForbidden()
    {
        _stadiumRepo.Setup(r => r.IsOwnedByAsync(1, "intruder", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.CreateAsync(new CreateCourtDto { Name = "New", StadiumId = 1 }, "intruder"));
    }
}
