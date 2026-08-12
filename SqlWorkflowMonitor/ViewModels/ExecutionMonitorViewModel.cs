using SqlWorkflowMonitor.DTOs;
using SqlWorkflowMonitor.Models;

namespace SqlWorkflowMonitor.ViewModels;

public sealed class ExecutionMonitorViewModel
{
    public List<ExecutionListItemDto> Executions { get; init; } = [];

    public List<ExecutionProcessOptionViewModel> Processes { get; init; } = [];

    public string? SelectedStatus { get; init; }

    public int? SelectedProcessId { get; init; }

    public DateTime? DateFrom { get; init; }

    public DateTime? DateTo { get; init; }

    public int RunningCount { get; init; }

    public int StaleRunningCount { get; init; }

    public int StaleThresholdMinutes { get; init; }

    public HashSet<long> StaleExecutionIds { get; init; } = [];

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public long? AverageDurationMs { get; init; }

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public string SortBy { get; init; } = "ExecutionId";

    public string SortDirection { get; init; } = "DESC";

    public ProductAccessStatus ProductAccess { get; init; } = new();
}

public sealed class ExecutionProcessOptionViewModel
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;
}
