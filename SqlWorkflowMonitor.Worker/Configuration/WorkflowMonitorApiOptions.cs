namespace SqlWorkflowMonitor.Worker.Configuration;

public sealed class WorkflowMonitorApiOptions
{
    public const string SectionName = "WorkflowMonitorApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
