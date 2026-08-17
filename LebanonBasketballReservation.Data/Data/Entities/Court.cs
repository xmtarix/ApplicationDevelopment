using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Data.Entities {
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsIndoor { get; set; }
        public int Capacity { get; set; }
        public decimal HourlyPrice { get; set; }
        public CourtStatus Status { get; set; } = CourtStatus.Active;
        public string? ImagePath { get; set; }

        public int StadiumId { get; set; }
        public Stadium Stadium { get; set; } = null!;

        public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    }
}
