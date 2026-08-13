using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Licensing.Models;
using SqlWorkflowMonitor.Licensing.Services;
using SqlWorkflowMonitor.Models;

namespace SqlWorkflowMonitor.Services;

public sealed class ProductAccessService
    : IProductAccessService
{
    private readonly ProductAccessRepository _repository;
    private readonly ILicenseValidator _licenseValidator;

    public ProductAccessService(
        ProductAccessRepository repository,
        ILicenseValidator licenseValidator)
    {
        _repository = repository;
        _licenseValidator = licenseValidator;
    }

    public async Task<ProductAccessStatus> GetStatusAsync(
        CancellationToken cancellationToken)
    {
        ProductAccessStatus currentUsage =
            await _repository.GetStatusAsync(
                cancellationToken);

        LicenseValidationResult validation =
            await _licenseValidator.ValidateAsync(
                cancellationToken);

        // Sin archivo de licencia continúa funcionando como Demo.
        if (validation.State ==
            LicenseValidationState.NotInstalled)
        {
            return currentUsage;
        }

        LicensePayload? license = validation.License;

        int daysRemaining =
            validation.State ==
                LicenseValidationState.GracePeriod
                ? validation.GraceDaysRemaining
                : validation.DaysRemaining;

        bool csvExportEnabled =
            license?.Features.Any(
                feature => string.Equals(
                    feature,
                    "csv-export",
                    StringComparison.Ordinal)) == true;

        return new ProductAccessStatus
        {
            Edition = license?.Edition ?? "Invalid",
            // INICIO AGREGADO - Estado de la licencia
            IsDemo = false,

            IsInGracePeriod =
    validation.State ==
    LicenseValidationState.GracePeriod,

            IsInvalidLicense =
    validation.State ==
    LicenseValidationState.Invalid,

            IsExpiringSoon =
    validation.IsExpiringSoon,

            AccessError =
    validation.Error,
            // FIN AGREGADO

            InstalledAtUtc =
                currentUsage.InstalledAtUtc,

            ExpiresAtUtc =
                license?.ExpiresUtc.UtcDateTime ??
                currentUsage.ExpiresAtUtc,

            IsExpired =
                !validation.CanStartExecutions,

            DaysRemaining =
                Math.Max(0, daysRemaining),

            MaxProcesses =
                license?.MaxProcesses ??
                currentUsage.MaxProcesses,

            RegisteredProcesses =
                currentUsage.RegisteredProcesses,

            MaxWorkers =
                license?.MaxWorkers ??
                currentUsage.MaxWorkers,

            RegisteredWorkers =
                currentUsage.RegisteredWorkers,

            CsvExportEnabled =
                csvExportEnabled
        };
    }

    public async Task ValidateCanStartAsync(
        int processId,
        string workerId,
        CancellationToken cancellationToken)
    {
        LicenseValidationResult validation =
            await _licenseValidator.ValidateAsync(
                cancellationToken);

        // Sin licencia comercial se aplican las reglas Demo.
        if (validation.State ==
            LicenseValidationState.NotInstalled)
        {
            await _repository.ValidateAndRegisterStartAsync(
                processId,
                workerId,
                cancellationToken);

            return;
        }

        if (!validation.CanStartExecutions ||
            validation.License is null)
        {
            throw new InvalidOperationException(
                validation.Error ??
                "La licencia no permite iniciar nuevas ejecuciones.");
        }

        LicensePayload license = validation.License;

        DateTimeOffset accessExpiresAtUtc =
            validation.State ==
                LicenseValidationState.GracePeriod
                ? license.ExpiresUtc.AddDays(
                    LicenseConstants.GracePeriodDays)
                : license.ExpiresUtc;

        await _repository.ValidateAndRegisterStartAsync(
            processId,
            workerId,
            license.MaxProcesses,
            license.MaxWorkers,
            accessExpiresAtUtc,
            cancellationToken);
    }
}