namespace SqlWorkflowMonitor.Licensing.Models;

public sealed class LicenseSignatureVerificationResult
{
    public bool IsValid { get; init; }

    public string? Error { get; init; }
}