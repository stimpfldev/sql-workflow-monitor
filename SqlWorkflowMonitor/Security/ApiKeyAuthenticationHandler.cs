using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SqlWorkflowMonitor.Security;

public sealed class ApiKeyAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    private readonly IOptionsMonitor<SecurityOptions> _securityOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<SecurityOptions> securityOptions)
        : base(options, logger, encoder)
    {
        _securityOptions = securityOptions;
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        string configuredApiKey =
            _securityOptions.CurrentValue.ApiKey;

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    "La API key no está configurada."));
        }

        if (!Request.Headers.TryGetValue(
                HeaderName,
                out var receivedValues))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        if (receivedValues.Count != 1)
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    "Debe enviarse una única API key."));
        }

        string receivedApiKey =
            receivedValues.ToString();

        if (!ApiKeysMatch(
                configuredApiKey,
                receivedApiKey))
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    "API key inválida."));
        }

        Claim[] claims =
        [
            new(
                ClaimTypes.NameIdentifier,
                "SqlWorkflowMonitorIntegration"),

            new(
                ClaimTypes.Name,
                "API Integration")
        ];

        var identity =
            new ClaimsIdentity(
                claims,
                SchemeName);

        var principal =
            new ClaimsPrincipal(identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }

    private static bool ApiKeysMatch(
        string configuredApiKey,
        string receivedApiKey)
    {
        byte[] configuredBytes =
            Encoding.UTF8.GetBytes(configuredApiKey);

        byte[] receivedBytes =
            Encoding.UTF8.GetBytes(receivedApiKey);

        return CryptographicOperations.FixedTimeEquals(
            configuredBytes,
            receivedBytes);
    }
}