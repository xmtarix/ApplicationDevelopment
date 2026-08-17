using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Web.ViewModels.Home;

public class HomeViewModel
{
    public IEnumerable<StadiumDto> FeaturedStadiums { get; set; } = Enumerable.Empty<StadiumDto>();
    public IEnumerable<GovernorateDto> Governorates { get; set; } = Enumerable.Empty<GovernorateDto>();
}
