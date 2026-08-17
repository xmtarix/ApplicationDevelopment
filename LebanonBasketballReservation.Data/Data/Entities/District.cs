
namespace LebanonBasketballReservation.Data.Entities {
    public class District
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int GovernorateId { get; set; }
        public Governorate Governorate { get; set; } = null!;

        public ICollection<Area> Areas { get; set; } = new List<Area>();
    }
}
