using LebanonBasketballReservation.Data.Enums;

namespace LebanonBasketballReservation.Data.Entities {
    public class Stadium
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public StadiumStatus Status { get; set; } = StadiumStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int AreaId { get; set; }
        public Area Area { get; set; } = null!;

        public string ManagerId { get; set; } = string.Empty;
        public ApplicationUser Manager { get; set; } = null!;

        public ICollection<Court> Courts { get; set; } = new List<Court>();
        public ICollection<StadiumImage> Images { get; set; } = new List<StadiumImage>();
        public ICollection<OpeningHour> OpeningHours { get; set; } = new List<OpeningHour>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
