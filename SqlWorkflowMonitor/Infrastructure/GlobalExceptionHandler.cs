using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SqlWorkflowMonitor.Services;

namespace SqlWorkflowMonitor.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ProductAccessDeniedException accessException)
        {
            _logger.LogWarning(
                accessException,
                "Se rechazó una operación por la política de acceso del producto.");

            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Acceso de producto restringido.",
                accessException.Message,
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "Se produjo un error no controlado al procesar la solicitud.");

        await WriteProblemDetailsAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor.",
            "Ocurrió un error inesperado al procesar la solicitud.",
            cancellationToken);

        return true;
    }

    private static Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);
    }
}
