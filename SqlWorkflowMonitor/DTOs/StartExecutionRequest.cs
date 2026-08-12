using System.ComponentModel.DataAnnotations;

namespace SqlWorkflowMonitor.DTOs;

public sealed class StartExecutionRequest
{
    [Range(1, int.MaxValue)]
    public int ProcessId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string WorkerId { get; init; } = string.Empty;
}