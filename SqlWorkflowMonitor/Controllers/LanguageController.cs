using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SqlWorkflowMonitor.Controllers;

[Route("language")]
public sealed class LanguageController : Controller
{
    [HttpPost("set")]
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

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/executions";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(returnUrl);
    }
}