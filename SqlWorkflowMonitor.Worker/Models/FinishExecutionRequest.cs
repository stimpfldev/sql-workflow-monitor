namespace SqlWorkflowMonitor.Worker.Models;

public sealed class FinishExecutionRequest
{
    public string Status { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public int? TotalItems { get; init; }

    public int? SucceededItems { get; init; }

    public int? FailedItems { get; init; }

    public int? AffectedRows { get; init; }
}