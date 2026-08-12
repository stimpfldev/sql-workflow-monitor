using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public interface ILicenseFileReader
{
    Task<LicenseFileReadResult> ReadAsync(
        CancellationToken cancellationToken);
}