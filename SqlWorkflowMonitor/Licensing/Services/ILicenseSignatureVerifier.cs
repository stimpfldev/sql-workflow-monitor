using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public interface ILicenseSignatureVerifier
{
    Task<LicenseSignatureVerificationResult> VerifyAsync(
        LicenseDocument document,
        CancellationToken cancellationToken);
}