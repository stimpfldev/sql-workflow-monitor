using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public interface ILicenseValidator
{
    Task<LicenseValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}