namespace SqlWorkflowMonitor.Licensing.Models;

public sealed class LicenseInstallationContext
{
    public Guid InstallationId { get; init; }

    public DateTimeOffset CurrentUtc { get; init; }
}