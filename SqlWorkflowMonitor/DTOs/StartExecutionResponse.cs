namespace SqlWorkflowMonitor.DTOs;

public sealed class StartExecutionResponse
{
    public long ExecutionId { get; init; }

    public string Status { get; init; } = "Running";
}