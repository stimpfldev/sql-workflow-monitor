using System.ComponentModel.DataAnnotations;

namespace SqlWorkflowMonitor.DTOs;

public sealed class FinishExecutionRequest
{
    [Required]
    [StringLength(20)]
    public string Status { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? ErrorMessage { get; init; }

    public int? TotalItems { get; init; }

    public int? SucceededItems { get; init; }

    public int? FailedItems { get; init; }

    public int? AffectedRows { get; init; }
}