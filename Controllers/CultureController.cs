// ====================================================================
// CultureController.cs : Contrôleur pour gérer le changement de culture
// ====================================================================

using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace CTSAR.Booking.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    private readonly ILogger<CultureController> _logger;

    public CultureController(ILogger<CultureController> logger)
    {
        _logger = logger;
    }

    public IActionResult Set(string culture, string redirectUri)
    {
        _logger.LogInformation($"Changement de culture demandé : {culture}, redirectUri: {redirectUri}");

        if (!string.IsNullOrEmpty(culture))
        {
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            _logger.LogInformation($"Cookie de culture défini pour : {culture}");
        }

        return LocalRedirect(redirectUri);
    }
}
