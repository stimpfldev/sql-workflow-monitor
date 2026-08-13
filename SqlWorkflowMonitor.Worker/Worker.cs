using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SqlWorkflowMonitor.Worker.Data;
using SqlWorkflowMonitor.Worker.Models;
using SqlWorkflowMonitor.Worker.Services;

namespace SqlWorkflowMonitor.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CustomerCsvProcessor _csvProcessor;
    private readonly StagingCustomerRepository _repository;
    private readonly IConfiguration _configuration;

    public Worker(
        ILogger<Worker> logger,
        IHttpClientFactory httpClientFactory,
        CustomerCsvProcessor csvProcessor,
        StagingCustomerRepository repository,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _csvProcessor = csvProcessor;
        _repository = repository;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        int intervalSeconds =
            _configuration.GetValue<int?>(
                "CsvImport:IntervalSeconds")
            ?? 60;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFileIfExistsAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Falló el procesamiento del CSV.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(intervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessFileIfExistsAsync(
        CancellationToken cancellationToken)
    {
        string inputFolder =
            GetRequiredSetting("CsvImport:InputFolder");

        string processedFolder =
            GetRequiredSetting("CsvImport:ProcessedFolder");

        string errorFolder =
            GetRequiredSetting("CsvImport:ErrorFolder");

        string fileName =
            GetRequiredSetting("CsvImport:FileName");

        int processId =
            _configuration.GetValue<int>(
                "CsvImport:ProcessId");

        string workerId =
            GetRequiredSetting("Worker:Id");

        if (processId <= 0)
        {
            throw new InvalidOperationException(
                "CsvImport:ProcessId debe ser mayor que cero.");
        }

        Directory.CreateDirectory(inputFolder);
        Directory.CreateDirectory(processedFolder);
        Directory.CreateDirectory(errorFolder);

        string filePath =
            Path.Combine(inputFolder, fileName);

        // 1. Buscar customers.csv
        if (!File.Exists(filePath))
        {
            _logger.LogInformation(
                "No se encontró el archivo {FileName}.",
                fileName);

            return;
        }

        HttpClient client =
            _httpClientFactory.CreateClient(
                "WorkflowMonitorApi");

        long? executionId = null;

        try
        {
            // 2. Iniciar la ejecución mediante la API
            executionId = await StartExecutionAsync(
                client,
                processId,
                workerId,
                cancellationToken);

            _logger.LogInformation(
                "Ejecución {ExecutionId} iniciada.",
                executionId);

            // 3 y 4. Leer y validar CSV
            List<StagingCustomer> customers =
                await _csvProcessor.ReadAndValidateAsync(
                    filePath,
                    cancellationToken);

            int validCount =
                customers.Count(customer =>
                    customer.IsValid);

            int invalidCount =
                customers.Count - validCount;

            // 5 y 6. Insertar staging y ejecutar el SP
            int insertedCustomers =
                await _repository.InsertAndProcessAsync(
                    executionId.Value,
                    customers,
                    cancellationToken);


            // 7. Finalizar como Succeeded
            _logger.LogInformation(
    "Métricas enviadas. Total={Total}, Correctos={Succeeded}, Incorrectos={Failed}, Afectados={Affected}",
    customers.Count,
    validCount,
    invalidCount,
    insertedCustomers);

            await FinishExecutionAsync(
                client,
                executionId.Value,
                "Succeeded",
                null,
                customers.Count,
                validCount,
                invalidCount,
                insertedCustomers,
                cancellationToken);

            // Mover a Processed solamente después de confirmar
            // que la ejecución quedó cerrada como Succeeded.
            MoveFile(
                filePath,
                processedFolder,
                executionId.Value);

            _logger.LogInformation(
                """
    Ejecución {ExecutionId} finalizada.
    Total: {Total}.
    Válidas: {Valid}.
    Inválidas: {Invalid}.
    Insertadas: {Inserted}.
    """,
                executionId,
                customers.Count,
                validCount,
                invalidCount,
                insertedCustomers);
        }
        catch (ProductAccessDeniedException exception)
        {
            _logger.LogWarning(
                exception,
                "El Worker quedó detenido por una restricción de la edición Demo. El archivo permanece en Input.");

            return;
        }
        catch (Exception exception)
        {
            // 8. Finalizar como Failed
            if (executionId.HasValue)
            {
                try
                {
                    using var finishTimeout =
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(10));

                    await FinishExecutionAsync(
      client,
      executionId.Value,
      "Failed",
      LimitErrorMessage(exception.Message),
      null,
      null,
      null,
      null,
      finishTimeout.Token);
                }
                catch (Exception finishException)
                {
                    _logger.LogError(
                        finishException,
                        """
                        No se pudo cerrar la ejecución
                        {ExecutionId} como Failed.
                        """,
                        executionId);
                }
            }

            MoveFileIfExists(
                filePath,
                errorFolder,
                executionId);

            throw;
        }
    }

    private static async Task<long> StartExecutionAsync(
        HttpClient client,
        int processId,
        string workerId,
        CancellationToken cancellationToken)
    {
        var request = new StartExecutionRequest
        {
            ProcessId = processId,
            WorkerId = workerId
        };

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "api/executions/start",
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            string detail =
                await ReadProblemDetailAsync(
                    response,
                    cancellationToken);

            throw new ProductAccessDeniedException(detail);
        }

        response.EnsureSuccessStatusCode();

        StartExecutionResponse? execution =
            await response.Content
                .ReadFromJsonAsync<StartExecutionResponse>(
                    cancellationToken);

        if (execution is null)
        {
            throw new InvalidOperationException(
                "La API no devolvió el ExecutionId.");
        }

        return execution.ExecutionId;
    }

    private static async Task<string> ReadProblemDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return "La edición Demo no permite iniciar esta ejecución.";
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(content);

            if (document.RootElement.TryGetProperty(
                    "detail",
                    out JsonElement detailElement))
            {
                return detailElement.GetString()
                    ?? content;
            }
        }
        catch (JsonException)
        {
            // Si la respuesta no es JSON, se utiliza el texto recibido.
        }

        return content;
    }

    private static async Task FinishExecutionAsync(
        HttpClient client,
        long executionId,
        string status,
        string? errorMessage,
        int? totalItems,
        int? succeededItems,
        int? failedItems,
        int? affectedRows,
        CancellationToken cancellationToken)
    {
        var request = new FinishExecutionRequest
        {
            Status = status,
            ErrorMessage = errorMessage,
            TotalItems = totalItems,
            SucceededItems = succeededItems,
            FailedItems = failedItems,
            AffectedRows = affectedRows
        };

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"api/executions/{executionId}/finish",
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private string GetRequiredSetting(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException(
                $"No se encontró la configuración '{key}'.");
    }

    private static string LimitErrorMessage(
        string errorMessage)
    {
        const int maximumLength = 2000;

        return errorMessage.Length <= maximumLength
            ? errorMessage
            : errorMessage[..maximumLength];
    }

    private static void MoveFile(
        string sourceFile,
        string destinationFolder,
        long executionId)
    {
        string extension =
            Path.GetExtension(sourceFile);

        string fileName =
            Path.GetFileNameWithoutExtension(
                sourceFile);

        string destinationFile =
            Path.Combine(
                destinationFolder,
                $"{fileName}_{executionId}_" +
                $"{DateTime.Now:yyyyMMdd_HHmmss}" +
                extension);

        File.Move(sourceFile, destinationFile);
    }

    private static void MoveFileIfExists(
        string sourceFile,
        string destinationFolder,
        long? executionId)
    {
        if (!File.Exists(sourceFile))
        {
            return;
        }

        string extension =
            Path.GetExtension(sourceFile);

        string fileName =
            Path.GetFileNameWithoutExtension(
                sourceFile);

        string identifier =
            executionId?.ToString()
            ?? "sin-ejecucion";

        string destinationFile =
            Path.Combine(
                destinationFolder,
                $"{fileName}_{identifier}_" +
                $"{DateTime.Now:yyyyMMdd_HHmmss}" +
                extension);

        File.Move(sourceFile, destinationFile);
    }

    private sealed class ProductAccessDeniedException
        : Exception
    {
        public ProductAccessDeniedException(string message)
            : base(message)
        {
        }
    }
}
