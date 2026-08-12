namespace SqlWorkflowMonitor.Worker.Configuration;

public sealed class CsvImportOptions
{
    public const string SectionName = "CsvImport";

    public int ProcessId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string InputFolder { get; set; } = string.Empty;

    public string ProcessedFolder { get; set; } = string.Empty;

    public string ErrorFolder { get; set; } = string.Empty;

    public int IntervalSeconds { get; set; } = 60;

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int MaxRows { get; set; } = 100_000;

    public int MinimumFileAgeSeconds { get; set; } = 5;
}
