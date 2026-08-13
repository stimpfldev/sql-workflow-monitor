namespace SqlWorkflowMonitor.DTOs;

public sealed class PagedExecutionResultDto
{
    public List<ExecutionListItemDto> Executions { get; init; } = [];

    public List<ExecutionProcessOptionDto> Processes { get; init; } = [];

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public int RunningCount { get; init; }

    public int StaleRunningCount { get; init; }

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public long? AverageDurationMs { get; init; }
}

public sealed class ExecutionProcessOptionDto
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;
}