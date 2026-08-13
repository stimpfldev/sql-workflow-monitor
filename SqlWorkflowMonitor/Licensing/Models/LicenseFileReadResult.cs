namespace SqlWorkflowMonitor.Licensing.Models;

public enum LicenseFileState
{
    NotFound,
    Loaded,
    Invalid
}

public sealed class LicenseFileReadResult
{
    public LicenseFileState State { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public LicenseDocument? Document { get; init; }

    public string? Error { get; init; }
}