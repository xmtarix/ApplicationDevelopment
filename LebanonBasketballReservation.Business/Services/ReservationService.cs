using LebanonBasketballReservation.Business.DTOs;
using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using LebanonBasketballReservation.Data.Enums;
using LebanonBasketballReservation.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LebanonBasketballReservation.Business.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ReservationService> _logger;
    private readonly int _cancellationWindowHours;

    public ReservationService(
        IUnitOfWork uow,
        INotificationService notifications,
        IDateTimeProvider clock,
        IConfiguration config,
        ILogger<ReservationService> logger)
    {
        _uow = uow;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
        _cancellationWindowHours = config.GetValue("ReservationSettings:CancellationWindowHours", 24);
    }

    private ReservationDto Map(Reservation r) => new()
    {
        Id = r.Id,
        Status = r.Status,
        TotalPrice = r.TotalPrice,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        CancelledAt = r.CancelledAt,
        CancellationReason = r.CancellationReason,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer?.FullName ?? "",
        CustomerEmail = r.Customer?.Email ?? "",
        CustomerPhone = r.Customer?.PhoneNumber,
        CourtName = r.TimeSlot?.Court?.Name ?? "",
        StadiumName = r.TimeSlot?.Court?.Stadium?.Name ?? "",
        StadiumAddress = r.TimeSlot?.Court?.Stadium?.Address ?? "",
        Date = r.TimeSlot?.Date ?? default,
        StartTime = r.TimeSlot?.StartTime ?? default,
        EndTime = r.TimeSlot?.EndTime ?? default,
        TimeSlotId = r.TimeSlotId,
        CourtId = r.TimeSlot?.CourtId ?? 0,
        StadiumId = r.TimeSlot?.Court?.StadiumId ?? 0,
        HasReview = r.Review is not null,
        CanCancel = IsWithinCancellationWindow(r)
    };

    /// <summary>
    /// The cancellation deadline is wall-clock time at the venue, so the slot's local start is
    /// converted to UTC before comparing against the current instant. Comparing a local
    /// DateTime to UtcNow directly would shift the window by the venue's offset.
    /// </summary>
    private bool IsWithinCancellationWindow(Reservation r)
    {
        if (r.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed)) return false;
        if (r.TimeSlot is null) return false;

        // Pending reservations are not yet committed by the manager, so they can always be withdrawn.
        if (r.Status == ReservationStatus.Pending) return true;

        var startUtc = _clock.ToUtc(r.TimeSlot.Date.ToDateTime(r.TimeSlot.StartTime));
        return _clock.UtcNow <= startUtc.AddHours(-_cancellationWindowHours);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationDto dto, string customerId, CancellationToken cancellationToken = default)
    {
        var slot = await _uow.TimeSlots.GetWithDetailsAsync(dto.TimeSlotId, cancellationToken)
            ?? throw NotFoundException.For("Time slot", dto.TimeSlotId);

        if (slot.Court is null || slot.Court.Stadium is null)
            throw new ConflictException("This time slot is not attached to a bookable court.");

        if (slot.Court.Stadium.Status != StadiumStatus.Active)
            throw new ConflictException("This stadium is not currently accepting reservations.");

        if (slot.Court.Status != CourtStatus.Active)
            throw new ConflictException("This court is not currently available for booking.");

        // Compare against venue-local now: a slot is bookable until the moment it starts.
        var startUtc = _clock.ToUtc(slot.Date.ToDateTime(slot.StartTime));
        if (startUtc <= _clock.UtcNow)
            throw new ConflictException("This time slot has already started and can no longer be booked.");

        if (slot.Reservation is not null &&
            slot.Reservation.Status is ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.Completed)
            throw new ConflictException("This time slot is no longer available.");

        var price = CalculatePrice(slot.Court.HourlyPrice, slot.StartTime, slot.EndTime);

        var reservationId = await _uow.ExecuteInTransactionAsync(async () =>
        {
            // Atomically flip IsAvailable. Only one concurrent caller wins, which is what
            // makes double-booking impossible rather than merely unlikely.
            if (!await _uow.TimeSlots.TryClaimAsync(dto.TimeSlotId, cancellationToken))
                throw new ConflictException("This time slot was just booked by someone else. Please pick another.");

            var reservation = new Reservation
            {
                CustomerId = customerId,
                TimeSlotId = dto.TimeSlotId,
                Status = ReservationStatus.Pending,
                TotalPrice = price,
                Notes = dto.Notes?.Trim(),
                CreatedAt = _clock.UtcNow
            };

            await _uow.Reservations.AddAsync(reservation, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return reservation.Id;
        }, cancellationToken);

        _logger.LogInformation("Reservation {ReservationId} created by {CustomerId} for slot {TimeSlotId}",
            reservationId, customerId, dto.TimeSlotId);

        await _notifications.CreateAsync(customerId, "Reservation submitted",
            $"Your booking for {slot.Court.Name} at {slot.Court.Stadium.Name} on {slot.Date:dd MMM yyyy} at {slot.StartTime:HH\\:mm} is awaiting confirmation.",
            NotificationType.General, reservationId, "Reservation", cancellationToken);

        // Tell the manager there is something to act on.
        await _notifications.CreateAsync(slot.Court.Stadium.ManagerId, "New reservation request",
            $"{slot.Court.Name} was requested for {slot.Date:dd MMM yyyy} at {slot.StartTime:HH\\:mm}.",
            NotificationType.General, reservationId, "Reservation", cancellationToken);

        var created = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken);
        return Map(created!);
    }

    private static decimal CalculatePrice(decimal hourlyPrice, TimeOnly start, TimeOnly end)
    {
        var hours = (decimal)(end - start).TotalHours;
        if (hours <= 0) hours = 1; // Defensive: a malformed slot still yields a sane charge.
        return Math.Round(hourlyPrice * hours, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<ReservationDto?> GetByIdAsync(int id, string userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var r = await _uow.Reservations.GetWithDetailsAsync(id, cancellationToken);
        if (r is null) return null;

        // Authorization lives here so every caller (MVC and API alike) is covered.
        var isCustomer = r.CustomerId == userId;
        var isManager = r.TimeSlot?.Court?.Stadium?.ManagerId == userId;
        if (!isAdmin && !isCustomer && !isManager) return null;

        return Map(r);
    }

    public async Task<IEnumerable<ReservationDto>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => (await _uow.Reservations.GetByCustomerAsync(customerId, cancellationToken)).Select(Map).ToList();

    public async Task<IEnumerable<ReservationDto>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default)
        => (await _uow.Reservations.GetByManagerAsync(managerId, cancellationToken)).Select(Map).ToList();

    public async Task<PagedResult<ReservationDto>> GetAllForAdminAsync(ReservationFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new ReservationFilterDto();

        var query = _uow.Reservations.QueryWithDetails();

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.StadiumId.HasValue)
            query = query.Where(r => r.TimeSlot.Court.StadiumId == filter.StadiumId.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(r => r.TimeSlot.Date >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(r => r.TimeSlot.Date <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r =>
                r.Customer.FirstName.Contains(term) ||
                r.Customer.LastName.Contains(term) ||
                r.Customer.Email!.Contains(term) ||
                r.TimeSlot.Court.Stadium.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.TimeSlot.Date).ThenByDescending(r => r.TimeSlot.StartTime).ThenByDescending(r => r.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> CanCancelAsync(int id, string customerId, CancellationToken cancellationToken = default)
    {
        var r = await _uow.Reservations.GetWithDetailsAsync(id, cancellationToken);
        return r is not null && r.CustomerId == customerId && IsWithinCancellationWindow(r);
    }

    public async Task CancelAsync(int id, string userId, string? reason, CancellationToken cancellationToken = default)
    {
        var r = await _uow.Reservations.GetWithDetailsAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Reservation", id);

        if (r.CustomerId != userId)
            throw new ForbiddenException("This reservation belongs to another customer.");

        if (r.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new ConflictException($"A {r.Status.ToString().ToLowerInvariant()} reservation cannot be cancelled.");

        if (!IsWithinCancellationWindow(r))
            throw new ConflictException($"Cancellations must be made at least {_cancellationWindowHours} hours before the reservation starts.");

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelledAt = _clock.UtcNow;
            r.CancellationReason = reason?.Trim();
            _uow.Reservations.Update(r);
            await _uow.SaveChangesAsync(cancellationToken);

            // Free the slot so someone else can book it.
            await _uow.TimeSlots.ReleaseAsync(r.TimeSlotId, cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Reservation {ReservationId} cancelled by {UserId}", id, userId);

        await _notifications.CreateAsync(r.CustomerId, "Reservation cancelled",
            $"Your reservation on {r.TimeSlot.Date:dd MMM yyyy} at {r.TimeSlot.StartTime:HH\\:mm} has been cancelled.",
            NotificationType.ReservationCancelled, r.Id, "Reservation", cancellationToken);

        var managerId = r.TimeSlot?.Court?.Stadium?.ManagerId;
        if (!string.IsNullOrEmpty(managerId))
        {
            await _notifications.CreateAsync(managerId, "Reservation cancelled",
                $"{r.Customer?.FullName ?? "A customer"} cancelled their booking on {r.TimeSlot!.Date:dd MMM yyyy}.",
                NotificationType.ReservationCancelled, r.Id, "Reservation", cancellationToken);
        }
    }

    public async Task ConfirmAsync(int id, string managerId, CancellationToken cancellationToken = default)
    {
        var r = await _uow.Reservations.GetWithDetailsAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Reservation", id);

        if (r.TimeSlot?.Court?.Stadium?.ManagerId != managerId)
            throw new ForbiddenException("This reservation belongs to a stadium you do not manage.");

        if (r.Status != ReservationStatus.Pending)
            throw new ConflictException("Only pending reservations can be confirmed.");

        r.Status = ReservationStatus.Confirmed;
        _uow.Reservations.Update(r);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reservation {ReservationId} confirmed by manager {ManagerId}", id, managerId);

        await _notifications.CreateAsync(r.CustomerId, "Reservation confirmed",
            $"Your booking for {r.TimeSlot.Court.Name} on {r.TimeSlot.Date:dd MMM yyyy} at {r.TimeSlot.StartTime:HH\\:mm} is confirmed. See you on the court!",
            NotificationType.ReservationConfirmed, r.Id, "Reservation", cancellationToken);
    }

    public async Task RejectAsync(int id, string managerId, string? reason, CancellationToken cancellationToken = default)
    {
        var r = await _uow.Reservations.GetWithDetailsAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Reservation", id);

        if (r.TimeSlot?.Court?.Stadium?.ManagerId != managerId)
            throw new ForbiddenException("This reservation belongs to a stadium you do not manage.");

        if (r.Status != ReservationStatus.Pending)
            throw new ConflictException("Only pending reservations can be rejected.");

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            r.Status = ReservationStatus.Rejected;
            r.CancellationReason = reason?.Trim();
            r.CancelledAt = _clock.UtcNow;
            _uow.Reservations.Update(r);
            await _uow.SaveChangesAsync(cancellationToken);

            await _uow.TimeSlots.ReleaseAsync(r.TimeSlotId, cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Reservation {ReservationId} rejected by manager {ManagerId}", id, managerId);

        var suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
        await _notifications.CreateAsync(r.CustomerId, "Reservation rejected",
            $"Your booking on {r.TimeSlot.Date:dd MMM yyyy} was not accepted.{suffix}",
            NotificationType.ReservationRejected, r.Id, "Reservation", cancellationToken);
    }

    public async Task SubmitReviewAsync(int reservationId, string customerId, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5 stars.");

        var r = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
            ?? throw NotFoundException.For("Reservation", reservationId);

        if (r.CustomerId != customerId)
            throw new ForbiddenException("This reservation belongs to another customer.");

        if (r.Status != ReservationStatus.Completed)
            throw new ConflictException("You can only review a reservation after it has been completed.");

        if (await _uow.Reviews.ExistsAsync(rv => rv.ReservationId == reservationId, cancellationToken))
            throw new ConflictException("You have already reviewed this reservation.");

        await _uow.Reviews.AddAsync(new Review
        {
            ReservationId = reservationId,
            CustomerId = customerId,
            StadiumId = r.TimeSlot.Court.StadiumId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = _clock.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review submitted for reservation {ReservationId} with rating {Rating}", reservationId, rating);

        var managerId = r.TimeSlot?.Court?.Stadium?.ManagerId;
        if (!string.IsNullOrEmpty(managerId))
        {
            await _notifications.CreateAsync(managerId, "New review",
                $"{r.Customer?.FullName ?? "A customer"} left a {rating}-star review for {r.TimeSlot!.Court.Stadium.Name}.",
                NotificationType.General, r.TimeSlot.Court.StadiumId, "Stadium", cancellationToken);
        }
    }
}
