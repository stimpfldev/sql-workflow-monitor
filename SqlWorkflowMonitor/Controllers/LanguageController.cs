using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SqlWorkflowMonitor.Controllers;

public class LanguageController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(
        string culture,
        string returnUrl)
    {
        string[] supportedCultures =
        {
            "es-AR",
            "en-US"
        };

        if (!supportedCultures.Contains(culture))
        {
            culture = "es-AR";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(returnUrl);
    }
}