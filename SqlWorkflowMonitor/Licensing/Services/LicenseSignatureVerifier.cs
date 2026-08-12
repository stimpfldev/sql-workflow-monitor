using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public sealed class LicenseSignatureVerifier
    : ILicenseSignatureVerifier
{
    private const long MaxPublicKeyFileSizeBytes = 32 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly LicenseOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LicenseSignatureVerifier> _logger;

    public LicenseSignatureVerifier(
        IOptions<LicenseOptions> options,
        IHostEnvironment environment,
        ILogger<LicenseSignatureVerifier> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<LicenseSignatureVerificationResult> VerifyAsync(
        LicenseDocument document,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Signature))
        {
            return Invalid(
                "The license signature is missing.");
        }

        string publicKeyFilePath = GetPublicKeyFilePath();

        if (!File.Exists(publicKeyFilePath))
        {
            return Invalid(
                "The public license key was not found.");
        }

        try
        {
            FileInfo fileInfo = new(publicKeyFilePath);

            if (fileInfo.Length == 0 ||
                fileInfo.Length > MaxPublicKeyFileSizeBytes)
            {
                return Invalid(
                    "The public license key is empty or invalid.");
            }

            string publicKeyPem =
                await File.ReadAllTextAsync(
                    publicKeyFilePath,
                    cancellationToken);

            byte[] signatureBytes =
                Convert.FromBase64String(
                    document.Signature.Trim());

            byte[] payloadBytes =
                JsonSerializer.SerializeToUtf8Bytes(
                    document.License,
                    JsonOptions);

            using RSA rsa = RSA.Create();

            rsa.ImportFromPem(publicKeyPem);

            bool isValid = rsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return isValid
                ? new LicenseSignatureVerificationResult
                {
                    IsValid = true
                }
                : Invalid(
                    "The license signature is invalid.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(
                exception,
                "The license signature is not valid Base64.");

            return Invalid(
                "The license signature format is invalid.");
        }
        catch (CryptographicException exception)
        {
            _logger.LogWarning(
                exception,
                "The license signature or public key is invalid.");

            return Invalid(
                "The license signature or public key is invalid.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "The public license key could not be read.");

            return Invalid(
                "The public license key could not be read.");
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Access to the public license key was denied.");

            return Invalid(
                "Access to the public license key was denied.");
        }
    }

    private string GetPublicKeyFilePath()
    {
        string configuredPath =
            string.IsNullOrWhiteSpace(_options.PublicKeyFilePath)
                ? LicenseOptions.DefaultPublicKeyFilePath
                : _options.PublicKeyFilePath.Trim();

        return Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    configuredPath));
    }

    private static LicenseSignatureVerificationResult Invalid(
        string error)
    {
        return new LicenseSignatureVerificationResult
        {
            IsValid = false,
            Error = error
        };
    }
}