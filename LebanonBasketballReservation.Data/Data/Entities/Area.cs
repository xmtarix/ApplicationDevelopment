
namespace LebanonBasketballReservation.Data.Entities {
    public class Area
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int DistrictId { get; set; }
        public District District { get; set; } = null!;

        public ICollection<Stadium> Stadiums { get; set; } = new List<Stadium>();
    }
}
