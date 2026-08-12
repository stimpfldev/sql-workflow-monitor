namespace SqlWorkflowMonitor.DTOs;

public sealed class ExecutionDetailDto
{
    public long ExecutionId { get; init; }

    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public long? DurationMs { get; init; }

    public string? ErrorMessage { get; init; }

    public int? TotalItems { get; init; }

    public int? SucceededItems { get; init; }

    public int? FailedItems { get; init; }

    public int? AffectedRows { get; init; }
}