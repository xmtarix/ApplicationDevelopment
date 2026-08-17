
namespace LebanonBasketballReservation.Data.Entities {
    public class Governorate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
