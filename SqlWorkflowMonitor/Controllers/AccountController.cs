using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlWorkflowMonitor.ViewModels;

namespace SqlWorkflowMonitor.Controllers;

[Route("account")]
public sealed class AccountController : Controller
{
    private const int PasswordIterations = 210_000;
    private const int PasswordHashSize = 32;

    private readonly IConfiguration _configuration;

    public AccountController(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(
        string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/executions");
        }

        return View(
            "~/Views/Account/Login.cshtml",
            new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Account/Login.cshtml",
                model);
        }

        string? configuredUsername =
            _configuration["Security:Admin:Username"];

        string? configuredPasswordHash =
            _configuration["Security:Admin:PasswordHash"];

        string? configuredPasswordSalt =
            _configuration["Security:Admin:PasswordSalt"];

        if (string.IsNullOrWhiteSpace(configuredUsername) ||
            string.IsNullOrWhiteSpace(configuredPasswordHash) ||
            string.IsNullOrWhiteSpace(configuredPasswordSalt))
        {
            model.ErrorMessage =
                "El administrador no está configurado.";

            return View(
                "~/Views/Account/Login.cshtml",
                model);
        }

        bool usernameIsValid =
            SecureTextEquals(
                model.Username,
                configuredUsername);

        bool passwordIsValid =
            VerifyPassword(
                model.Password,
                configuredPasswordHash,
                configuredPasswordSalt);

        if (!usernameIsValid ||
            !passwordIsValid)
        {
            model.ErrorMessage =
                "Usuario o contraseña incorrectos.";

            return View(
                "~/Views/Account/Login.cshtml",
                model);
        }

        Claim[] claims =
        [
            new(
                ClaimTypes.NameIdentifier,
                configuredUsername),

            new(
                ClaimTypes.Name,
                configuredUsername),

            new(
                ClaimTypes.Role,
                "Administrator")
        ];

        var identity =
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
            Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return Redirect("/executions");
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return StatusCode(
            StatusCodes.Status403Forbidden);
    }

    private static bool VerifyPassword(
        string password,
        string expectedHashBase64,
        string saltBase64)
    {
        try
        {
            byte[] salt =
                Convert.FromBase64String(saltBase64);

            byte[] expectedHash =
                Convert.FromBase64String(
                    expectedHashBase64);

            byte[] actualHash =
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    PasswordIterations,
                    HashAlgorithmName.SHA256,
                    PasswordHashSize);

            return CryptographicOperations
                .FixedTimeEquals(
                    actualHash,
                    expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool SecureTextEquals(
        string first,
        string second)
    {
        byte[] firstBytes =
            Encoding.UTF8.GetBytes(first);

        byte[] secondBytes =
            Encoding.UTF8.GetBytes(second);

        return CryptographicOperations
            .FixedTimeEquals(
                firstBytes,
                secondBytes);
    }
}