using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.DTOs;
using SqlWorkflowMonitor.Models;
using SqlWorkflowMonitor.Services;
using SqlWorkflowMonitor.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace SqlWorkflowMonitor.Controllers;

[Authorize]
[Route("executions")]
public sealed class ExecutionDashboardController : Controller
{
    private readonly ExecutionRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly IProductAccessService _productAccess;

    public ExecutionDashboardController(
        ExecutionRepository repository,
        IConfiguration configuration,
        IProductAccessService productAccess)
    {
        _repository = repository;
        _configuration = configuration;
        _productAccess = productAccess;
    }

    // GET /executions
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status,
        int? processId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "ExecutionId",
        string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize != 10 &&
            pageSize != 25 &&
            pageSize != 50)
        {
            pageSize = 10;
        }

        (sortBy, sortDirection) =
            ValidateSort(sortBy, sortDirection);

        int staleThresholdMinutes =
            _configuration.GetValue<int?>(
                "Monitoring:StaleExecutionMinutes")
            ?? 10;

        PagedExecutionResultDto result =
            await _repository.GetPagedAsync(
                status,
                processId,
                dateFrom,
                dateTo,
                pageNumber,
                pageSize,
                sortBy,
                sortDirection,
                staleThresholdMinutes,
                cancellationToken);

        ProductAccessStatus productAccess =
            await _productAccess.GetStatusAsync(
                cancellationToken);

        DateTime staleLimit =
            DateTime.UtcNow.AddMinutes(
                -staleThresholdMinutes);

        HashSet<long> staleExecutionIds =
            result.Executions
                .Where(execution =>
                    execution.Status.Equals(
                        "Running",
                        StringComparison.OrdinalIgnoreCase)
                    && execution.FinishedAt is null
                    && execution.StartedAt < staleLimit)
                .Select(execution => execution.ExecutionId)
                .ToHashSet();

        var model = new ExecutionMonitorViewModel
        {
            Executions = result.Executions,

            Processes = result.Processes
                .Select(process =>
                    new ExecutionProcessOptionViewModel
                    {
                        ProcessId = process.ProcessId,
                        ProcessName = process.ProcessName
                    })
                .ToList(),

            SelectedStatus = status,
            SelectedProcessId = processId,
            DateFrom = dateFrom,
            DateTo = dateTo,

            RunningCount = result.RunningCount,
            StaleRunningCount = result.StaleRunningCount,
            SucceededCount = result.SucceededCount,
            FailedCount = result.FailedCount,
            AverageDurationMs = result.AverageDurationMs,

            StaleThresholdMinutes = staleThresholdMinutes,
            StaleExecutionIds = staleExecutionIds,

            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            SortBy = sortBy,
            SortDirection = sortDirection,

            ProductAccess = productAccess
        };

        return View(
            "~/Views/Executions/Index.cshtml",
            model);
    }

    // GET /executions/export/csv
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
    string? status,
    int? processId,
    DateTime? dateFrom,
    DateTime? dateTo,
    string sortBy = "ExecutionId",
    string sortDirection = "DESC",
    CancellationToken cancellationToken = default)
    {
        ProductAccessStatus productAccess =
            await _productAccess.GetStatusAsync(
                cancellationToken);

        if (!productAccess.CsvExportEnabled)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Exportación CSV no disponible.",
                    Detail =
                        "La licencia actual no permite exportar archivos CSV."
                });
        }

        (sortBy, sortDirection) =
            ValidateSort(sortBy, sortDirection);


        List<ExecutionListItemDto> executions =
       await _repository.GetForExportAsync(
    status,
    processId,
    dateFrom,
    dateTo,
    sortBy,
    sortDirection,
    5000,
    cancellationToken);

        bool isSpanish =
            CultureInfo.CurrentUICulture
                .TwoLetterISOLanguageName
                .Equals(
                    "es",
                    StringComparison.OrdinalIgnoreCase);

        string dateFormat = isSpanish
            ? "dd/MM/yyyy HH:mm:ss"
            : "MM/dd/yyyy HH:mm:ss";

        var csv = new StringBuilder();

        // Excel interpreta correctamente el separador en ambas configuraciones.
        csv.AppendLine("sep=;");

        csv.AppendLine(
            isSpanish
                ? "ID;Proceso;Estado;Inicio;Finalización;Duración (ms)"
                : "ID;Process;Status;Started;Finished;Duration (ms)");

        foreach (ExecutionListItemDto execution in executions)
        {
            csv.Append(
                execution.ExecutionId.ToString(
                    CultureInfo.InvariantCulture));
            csv.Append(';');
            csv.Append(
                ToCsvText(
                    TranslateProcessName(
                        execution.ProcessName,
                        isSpanish)));
            csv.Append(';');
            csv.Append(
                ToCsvText(
                    TranslateStatus(
                        execution.Status,
                        isSpanish)));
            csv.Append(';');
            csv.Append(
                ToCsvText(
                    execution.StartedAt.ToString(
                        dateFormat,
                        CultureInfo.InvariantCulture)));
            csv.Append(';');
            csv.Append(
                ToCsvText(
                    execution.FinishedAt?.ToString(
                        dateFormat,
                        CultureInfo.InvariantCulture)));
            csv.Append(';');

            if (execution.DurationMs.HasValue)
            {
                csv.Append(
                    execution.DurationMs.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            csv.AppendLine();
        }

        byte[] content =
            Encoding.UTF8.GetBytes(csv.ToString());

        byte[] preamble =
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true)
            .GetPreamble();

        byte[] fileBytes =
            new byte[preamble.Length + content.Length];

        Buffer.BlockCopy(
            preamble,
            0,
            fileBytes,
            0,
            preamble.Length);

        Buffer.BlockCopy(
            content,
            0,
            fileBytes,
            preamble.Length,
            content.Length);

        string fileName =
            $"executions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(
            fileBytes,
            "text/csv; charset=utf-8",
            fileName);
    }

    // GET /executions/2
    [HttpGet("{executionId:long}")]
    public async Task<IActionResult> Details(
        long executionId,
        CancellationToken cancellationToken)
    {
        ExecutionDetailDto? execution =
            await _repository.GetByIdAsync(
                executionId,
                cancellationToken);

        if (execution is null)
        {
            return NotFound();
        }

        return View(
            "~/Views/Executions/Details.cshtml",
            execution);
    }

    private static (string SortBy, string SortDirection)
        ValidateSort(
            string? sortBy,
            string? sortDirection)
    {
        string[] allowedSortColumns =
        [
            "ExecutionId",
            "ProcessName",
            "Status",
            "StartedAt",
            "FinishedAt",
            "DurationMs"
        ];

        string validSortBy =
            allowedSortColumns.FirstOrDefault(column =>
                column.Equals(
                    sortBy,
                    StringComparison.OrdinalIgnoreCase))
            ?? "ExecutionId";

        string validSortDirection =
            string.Equals(
                sortDirection,
                "ASC",
                StringComparison.OrdinalIgnoreCase)
                ? "ASC"
                : "DESC";

        return (validSortBy, validSortDirection);
    }

    private static string ToCsvText(string? value)
    {
        value ??= string.Empty;

        // Evita que Excel interprete texto externo como una fórmula.
        if (value.Length > 0 &&
            value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = $"'{value}";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string TranslateStatus(
        string status,
        bool isSpanish)
    {
        if (!isSpanish)
        {
            return status;
        }

        return status.ToLowerInvariant() switch
        {
            "running" => "En ejecución",
            "succeeded" => "Exitosa",
            "failed" => "Fallida",
            "cancelled" => "Cancelada",
            _ => status
        };
    }

    private static string TranslateProcessName(
        string processName,
        bool isSpanish)
    {
        if (isSpanish)
        {
            return processName;
        }

        return processName switch
        {
            "Generación de reporte diario" =>
                "Daily report generation",
            "Importación de clientes" =>
                "Customer import",
            _ => processName
        };
    }
}
