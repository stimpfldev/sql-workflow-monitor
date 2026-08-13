namespace SqlWorkflowMonitor.DTOs;

public sealed class ExecutionListItemDto
{
    public long ExecutionId { get; init; }

    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public long? DurationMs { get; init; }
}