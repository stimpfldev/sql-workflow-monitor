using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Services;

namespace SqlWorkflowMonitor.Controllers;

using SqlWorkflowMonitor.DTOs;
using SqlWorkflowMonitor.Security;

[ApiController]
[Route("api/executions")]
[Authorize(
    AuthenticationSchemes =
        ApiKeyAuthenticationHandler.SchemeName)]
public sealed class ExecutionsController : ControllerBase
{
    private static readonly string[] AllowedFinalStatuses =
    [
        "Succeeded",
        "Failed",
           "Cancelled"
    ];

    private readonly ExecutionRepository _repository;
    private readonly IProductAccessService _productAccess;

    public ExecutionsController(
        ExecutionRepository repository,
        IProductAccessService productAccess)
    {
        _repository = repository;
        _productAccess = productAccess;
    }

    [HttpPost("start")]
    [ProducesResponseType<StartExecutionResponse>(
      StatusCodes.Status200OK)]
    [ProducesResponseType(
      StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
      StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StartExecutionResponse>> Start(
      StartExecutionRequest request,
      CancellationToken cancellationToken)
    {
        try
        {
            await _productAccess.ValidateCanStartAsync(
                request.ProcessId,
                request.WorkerId,
                cancellationToken);

            long executionId =
                await _repository.StartAsync(
                    request.ProcessId,
                    cancellationToken);

            var response =
                new StartExecutionResponse
                {
                    ExecutionId = executionId,
                    Status = "Running"
                };

            return Ok(response);
        }
        catch (SqlException ex)
            when (ex.Number == 50001)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No se pudo iniciar la ejecución.",
                Detail = ex.Message
            });
        }
        catch (SqlException ex)
            when (ex.Number is 50010 or 50011 or 50012 or 50013 or 50014)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Restricción de la edición Demo.",
                    Detail = ex.Message
                });
        }
    } // Cierra el método Start

    [HttpPost("{executionId:long}/finish")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finish(
        long executionId,
        FinishExecutionRequest request,
        CancellationToken cancellationToken)
    {
        bool validStatus =
            AllowedFinalStatuses.Contains(
                request.Status,
                StringComparer.OrdinalIgnoreCase);

        if (!validStatus)
        {
            ModelState.AddModelError(
                nameof(request.Status),
                "El estado debe ser 'Succeeded', 'Failed' o 'Cancelled'.");

            return ValidationProblem(ModelState);
        }

        if (request.Status.Equals(
                "Failed",
                StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(
                request.ErrorMessage))
        {
            ModelState.AddModelError(
                nameof(request.ErrorMessage),
                "Una ejecución fallida debe indicar el error.");

            return ValidationProblem(ModelState);
        }

        if (request.TotalItems < 0
            || request.SucceededItems < 0
            || request.FailedItems < 0
            || request.AffectedRows < 0)
        {
            ModelState.AddModelError(
                "Metrics",
                "Las métricas no pueden ser negativas.");

            return ValidationProblem(ModelState);
        }
        if (request.TotalItems.HasValue
       && (long)request.SucceededItems.GetValueOrDefault()
           + request.FailedItems.GetValueOrDefault()
           > request.TotalItems.Value)
        {
            ModelState.AddModelError(
                "Metrics",
                "La suma de elementos exitosos y fallidos no puede superar el total.");

            return ValidationProblem(ModelState);
        }
        try
        {
            await _repository.FinishAsync(
                executionId,
                request.Status,
                request.ErrorMessage,
                request.TotalItems,
                request.SucceededItems,
                request.FailedItems,
                request.AffectedRows,
                cancellationToken);

            return NoContent();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
            when (ex.Number == 50002
               || ex.Number == 50003
               || ex.Number == 50004)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No se pudo finalizar la ejecución.",
                Detail = ex.Message
            });
        }
    } // FIN DEL MÉTODO Finish

    [HttpGet]
    [ProducesResponseType<List<ExecutionListItemDto>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExecutionListItemDto>>> GetAll(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        List<ExecutionListItemDto> executions =
            await _repository.GetAllAsync(
                limit,
                cancellationToken);

        return Ok(executions);
    }

    [HttpGet("stuck")]
    [ProducesResponseType<List<StuckExecutionDto>>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    public async Task<
    ActionResult<List<StuckExecutionDto>>> GetStuck(
    [FromQuery] int olderThanMinutes = 30,
    CancellationToken cancellationToken = default)
    {
        if (olderThanMinutes <= 0)
        {
            ModelState.AddModelError(
                nameof(olderThanMinutes),
                "La cantidad de minutos debe ser mayor que cero.");

            return ValidationProblem(ModelState);
        }

        List<StuckExecutionDto> executions =
            await _repository.GetStuckAsync(
                olderThanMinutes,
                cancellationToken);

        return Ok(executions);
    }

    [HttpGet("{executionId:long}")]
    [ProducesResponseType<ExecutionDetailDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<ExecutionDetailDto>> GetById(
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

        return Ok(execution);
    }
}
