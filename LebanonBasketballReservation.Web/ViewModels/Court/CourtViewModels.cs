using LebanonBasketballReservation.Business.DTOs;

namespace LebanonBasketballReservation.Web.ViewModels.Court;

public class CourtListViewModel
{
    public StadiumDto Stadium { get; set; } = null!;
    public IEnumerable<CourtDto> Courts { get; set; } = Enumerable.Empty<CourtDto>();
}

public class TimeSlotManagerViewModel
{
    public CourtDto Court { get; set; } = null!;
    public DateOnly Date { get; set; }
    public IEnumerable<TimeSlotDto> Slots { get; set; } = Enumerable.Empty<TimeSlotDto>();
    public GenerateTimeSlotsDto GenerateForm { get; set; } = new();
}
