namespace SqlWorkflowMonitor.Worker.Configuration;

public sealed class WorkerIdentityOptions
{
    public const string SectionName = "Worker";

    public string Id { get; set; } = string.Empty;
}
