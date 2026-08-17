
namespace LebanonBasketballReservation.Data.Entities {
    public class StadiumImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }

        public int StadiumId { get; set; }
        public Stadium Stadium { get; set; } = null!;
    }
}
