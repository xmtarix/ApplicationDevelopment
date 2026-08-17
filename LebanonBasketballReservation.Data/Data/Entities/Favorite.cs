
namespace LebanonBasketballReservation.Data.Entities {
    public class Favorite
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = null!;

        public int StadiumId { get; set; }
        public Stadium Stadium { get; set; } = null!;
    }
}
