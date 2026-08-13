using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public sealed class LicenseValidator : ILicenseValidator
{
    private readonly ILicenseFileReader _fileReader;
    private readonly ILicenseSignatureVerifier _signatureVerifier;
    private readonly ProductAccessRepository _repository;

    public LicenseValidator(
        ILicenseFileReader fileReader,
        ILicenseSignatureVerifier signatureVerifier,
        ProductAccessRepository repository)
    {
        _fileReader = fileReader;
        _signatureVerifier = signatureVerifier;
        _repository = repository;
    }

    public async Task<LicenseValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        LicenseFileReadResult fileResult =
            await _fileReader.ReadAsync(cancellationToken);

        if (fileResult.State == LicenseFileState.NotFound)
        {
            return new LicenseValidationResult
            {
                State = LicenseValidationState.NotInstalled
            };
        }

        if (fileResult.State == LicenseFileState.Invalid ||
            fileResult.Document is null)
        {
            return Invalid(
                fileResult.Error ??
                "El archivo de licencia no es válido.");
        }

        LicenseSignatureVerificationResult signatureResult =
            await _signatureVerifier.VerifyAsync(
                fileResult.Document,
                cancellationToken);

        if (!signatureResult.IsValid)
        {
            return Invalid(
                signatureResult.Error ??
                "La firma de la licencia no es válida.");
        }

        LicensePayload license = fileResult.Document.License;

        LicenseInstallationContext installation =
            await _repository
                .GetLicenseInstallationContextAsync(
                    cancellationToken);

        string? contractError =
            ValidateContract(license, installation);

        if (contractError is not null)
        {
            return Invalid(contractError, installation.CurrentUtc);
        }

        if (installation.CurrentUtc <= license.ExpiresUtc)
        {
            int daysRemaining = Math.Max(
                1,
                (int)Math.Ceiling(
                    (license.ExpiresUtc -
                     installation.CurrentUtc).TotalDays));

            return new LicenseValidationResult
            {
                State = LicenseValidationState.Valid,
                License = license,
                CurrentUtc = installation.CurrentUtc,
                DaysRemaining = daysRemaining,
                IsExpiringSoon =
                    daysRemaining <=
                    LicenseConstants.ExpirationWarningDays
            };
        }

        DateTimeOffset graceEndsUtc =
            license.ExpiresUtc.AddDays(
                LicenseConstants.GracePeriodDays);

        if (installation.CurrentUtc <= graceEndsUtc)
        {
            int graceDaysRemaining = Math.Max(
                1,
                (int)Math.Ceiling(
                    (graceEndsUtc -
                     installation.CurrentUtc).TotalDays));

            return new LicenseValidationResult
            {
                State = LicenseValidationState.GracePeriod,
                License = license,
                CurrentUtc = installation.CurrentUtc,
                GraceDaysRemaining = graceDaysRemaining,
                IsExpiringSoon = true
            };
        }

        return new LicenseValidationResult
        {
            State = LicenseValidationState.Expired,
            License = license,
            CurrentUtc = installation.CurrentUtc,
            IsExpiringSoon = true,
            Error =
                "La licencia venció y finalizó su período de gracia."
        };
    }

    private static string? ValidateContract(
        LicensePayload license,
        LicenseInstallationContext installation)
    {
        if (!string.Equals(
                license.Product,
                LicenseConstants.ProductName,
                StringComparison.Ordinal))
        {
            return "La licencia pertenece a otro producto.";
        }

        if (license.SchemaVersion !=
            LicenseConstants.SchemaVersion)
        {
            return "La versión de la licencia no es compatible.";
        }

        if (license.LicenseId == Guid.Empty)
        {
            return "La licencia no tiene un LicenseId válido.";
        }

        if (string.IsNullOrWhiteSpace(license.Customer))
        {
            return "La licencia no tiene un cliente válido.";
        }

        bool validEdition =
            string.Equals(
                license.Edition,
                LicenseEditions.Professional,
                StringComparison.Ordinal) ||
            string.Equals(
                license.Edition,
                LicenseEditions.Enterprise,
                StringComparison.Ordinal);

        if (!validEdition)
        {
            return "La edición de la licencia no es válida.";
        }

        if (license.InstallationId !=
            installation.InstallationId)
        {
            return
                "La licencia pertenece a otra instalación.";
        }

        if (license.IssuedUtc.Offset != TimeSpan.Zero ||
            license.ExpiresUtc.Offset != TimeSpan.Zero)
        {
            return "Las fechas de la licencia deben estar en UTC.";
        }

        if (license.IssuedUtc >
            installation.CurrentUtc.AddMinutes(5))
        {
            return "La fecha de emisión de la licencia no es válida.";
        }

        if (license.ExpiresUtc <= license.IssuedUtc)
        {
            return "El vencimiento de la licencia no es válido.";
        }

        if (license.MaxProcesses <= 0 ||
            license.MaxWorkers <= 0)
        {
            return "Los límites de la licencia no son válidos.";
        }

        if (license.Features is null)
        {
            return "La licencia no contiene funcionalidades.";
        }

        return null;
    }

    private static LicenseValidationResult Invalid(
        string error,
        DateTimeOffset currentUtc = default)
    {
        return new LicenseValidationResult
        {
            State = LicenseValidationState.Invalid,
            CurrentUtc = currentUtc,
            Error = error
        };
    }
}