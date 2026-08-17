using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Web.Models;
using LebanonBasketballReservation.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LebanonBasketballReservation.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStadiumService _stadiums;
    private readonly ILocationService _locations;

    public HomeController(IStadiumService stadiums, ILocationService locations)
    {
        _stadiums = stadiums;
        _locations = locations;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new HomeViewModel
        {
            FeaturedStadiums = await _stadiums.GetFeaturedAsync(6, cancellationToken),
            Governorates = await _locations.GetGovernoratesAsync(cancellationToken)
        };

        return View(vm);
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code ?? 500
        });
    }

    /// <summary>
    /// Target for UseStatusCodePagesWithReExecute, so 404s render the branded page.
    /// Named HttpStatus rather than StatusCode to avoid hiding ControllerBase.StatusCode(int).
    /// </summary>
    [ActionName("StatusCode")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int? code)
    {
        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code ?? 404
        });
    }
}
