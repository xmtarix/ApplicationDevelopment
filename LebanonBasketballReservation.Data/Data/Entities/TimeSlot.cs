
namespace LebanonBasketballReservation.Data.Entities {
    public class TimeSlot
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;

        public int CourtId { get; set; }
        public Court Court { get; set; } = null!;

        public Reservation? Reservation { get; set; }
    }
}
