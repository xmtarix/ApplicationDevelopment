using LebanonBasketballReservation.Business.Exceptions;
using LebanonBasketballReservation.Business.Interfaces;
using LebanonBasketballReservation.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LebanonBasketballReservation.Web.Areas.Customer.Controllers;

public class FavoritesController : CustomerBaseController
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(
        IFavoriteService favorites,
        UserManager<ApplicationUser> userManager) : base(userManager)
        => _favorites = favorites;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _favorites.GetFavoritesAsync(CustomerId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Toggle(int stadiumId, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var added = await _favorites.ToggleAsync(CustomerId, stadiumId, cancellationToken);

            // AJAX callers just need the new state; full posts get a banner and a redirect.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { isFavorite = added });

            Success(added ? "Added to your favorites." : "Removed from your favorites.");
        }
        catch (DomainException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { error = ex.Message });

            Error(ex.Message);
        }

        // Only ever redirect within this site.
        return returnUrl is not null && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Details", "Stadiums", new { area = "", id = stadiumId });
    }
}
