
namespace LebanonBasketballReservation.Data.Entities {
    public class OpeningHour
    {
        public int Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }

        public int StadiumId { get; set; }
        public Stadium Stadium { get; set; } = null!;
    }
}
