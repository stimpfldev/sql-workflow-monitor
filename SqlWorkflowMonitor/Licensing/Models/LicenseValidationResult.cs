namespace SqlWorkflowMonitor.Licensing.Models;

public enum LicenseValidationState
{
    NotInstalled,
    Valid,
    GracePeriod,
    Expired,
    Invalid
}

public sealed class LicenseValidationResult
{
    public LicenseValidationState State { get; init; }

    public LicensePayload? License { get; init; }

    public DateTimeOffset CurrentUtc { get; init; }

    public int DaysRemaining { get; init; }

    public int GraceDaysRemaining { get; init; }

    public bool IsExpiringSoon { get; init; }

    public string? Error { get; init; }

    public bool CanStartExecutions =>
        State is LicenseValidationState.Valid
            or LicenseValidationState.GracePeriod;

    public bool IsReadOnly =>
        State is LicenseValidationState.Expired
            or LicenseValidationState.Invalid;
}