namespace SqlWorkflowMonitor.Worker.Models;

public sealed class StagingCustomer
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public bool IsValid { get; init; }

    public string? ValidationError { get; init; }
}