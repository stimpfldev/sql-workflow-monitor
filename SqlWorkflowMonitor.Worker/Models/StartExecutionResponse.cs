namespace SqlWorkflowMonitor.Worker.Models;

public sealed class StartExecutionResponse
{
    public long ExecutionId { get; init; }

    public string Status { get; init; } = string.Empty;
}