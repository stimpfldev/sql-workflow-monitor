using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SqlWorkflowMonitor.Data;

namespace SqlWorkflowMonitor.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly ExecutionRepository _repository;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ExecutionRepository repository,
        ILogger<HealthController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        try
        {
            await _repository.CheckDatabaseConnectionAsync(
                cancellationToken);

            return Ok(new
            {
                status = "Healthy",
                database = "Connected",
                checkedAtUtc = DateTime.UtcNow
            });
        }
        catch (SqlException exception)
        {
            _logger.LogError(
                exception,
                "El health check no pudo conectarse con la base de datos.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status503ServiceUnavailable,
                    Title = "Servicio no disponible.",
                    Detail =
                        "No fue posible conectarse con la base de datos."
                });
        }
    }
}