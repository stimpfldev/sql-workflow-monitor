namespace SqlWorkflowMonitor.Worker.Models;

public sealed class StartExecutionRequest
{
    public int ProcessId { get; init; }

    public string WorkerId { get; init; } = string.Empty;
}
