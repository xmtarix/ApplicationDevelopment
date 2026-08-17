using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Data.Entities {
    public class Reservation
    {
        public int Id { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public bool ReminderSent { get; set; }

        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = null!;

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public Review? Review { get; set; }
    }
}
