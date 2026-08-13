using System.Data;
using Microsoft.Data.SqlClient;
using SqlWorkflowMonitor.DTOs;

namespace SqlWorkflowMonitor.Data;

public sealed class ExecutionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ExecutionRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> StartAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_Start",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@ProcessId", SqlDbType.Int)
            .Value = processId;

        await connection.OpenAsync(cancellationToken);

        object? result =
            await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException(
                "El stored procedure no devolvió el ExecutionId.");
        }

        return Convert.ToInt64(result);
    }

    public async Task FinishAsync(
        long executionId,
        string status,
        string? errorMessage,
        int? totalItems,
        int? succeededItems,
        int? failedItems,
        int? affectedRows,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_Finish",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@ExecutionId", SqlDbType.BigInt)
            .Value = executionId;

        command.Parameters
            .Add("@Status", SqlDbType.VarChar, 20)
            .Value = status;

        command.Parameters
            .Add("@ErrorMessage", SqlDbType.NVarChar, 2000)
            .Value = string.IsNullOrWhiteSpace(errorMessage)
                ? DBNull.Value
                : errorMessage;

        command.Parameters
            .Add("@TotalItems", SqlDbType.Int)
            .Value = (object?)totalItems ?? DBNull.Value;

        command.Parameters
            .Add("@SucceededItems", SqlDbType.Int)
            .Value = (object?)succeededItems ?? DBNull.Value;

        command.Parameters
            .Add("@FailedItems", SqlDbType.Int)
            .Value = (object?)failedItems ?? DBNull.Value;

        command.Parameters
            .Add("@AffectedRows", SqlDbType.Int)
            .Value = (object?)affectedRows ?? DBNull.Value;

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<ExecutionListItemDto>> GetAllAsync(
       int maxRows,
       CancellationToken cancellationToken)
    {
        var executions =
            new List<ExecutionListItemDto>();

        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_List",
            connection);

        command.CommandType = CommandType.StoredProcedure;
        command.Parameters
    .Add("@MaxRows", SqlDbType.Int)
    .Value = maxRows;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            executions.Add(
                new ExecutionListItemDto
                {
                    ExecutionId =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "ExecutionId")),

                    ProcessId =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "ProcessId")),

                    ProcessName =
                        reader.GetString(
                            reader.GetOrdinal(
                                "ProcessName")),

                    Status =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Status")),

                    StartedAt =
                        reader.GetDateTime(
                            reader.GetOrdinal(
                                "StartedAt")),

                    FinishedAt =
                        reader.IsDBNull(
                            reader.GetOrdinal(
                                "FinishedAt"))
                            ? null
                            : reader.GetDateTime(
                                reader.GetOrdinal(
                                    "FinishedAt")),

                    DurationMs =
                        reader.IsDBNull(
                            reader.GetOrdinal(
                                "DurationMs"))
                            ? null
                            : reader.GetInt64(
                                reader.GetOrdinal(
                                    "DurationMs"))
                });
        }

        return executions;
    }

    public async Task<ExecutionDetailDto?> GetByIdAsync(
        long executionId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_GetById",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@ExecutionId", SqlDbType.BigInt)
            .Value = executionId;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ExecutionDetailDto
        {
            ExecutionId =
                reader.GetInt64(
                    reader.GetOrdinal(
                        "ExecutionId")),

            ProcessId =
                reader.GetInt32(
                    reader.GetOrdinal(
                        "ProcessId")),

            ProcessName =
                reader.GetString(
                    reader.GetOrdinal(
                        "ProcessName")),

            Status =
                reader.GetString(
                    reader.GetOrdinal(
                        "Status")),

            StartedAt =
                reader.GetDateTime(
                    reader.GetOrdinal(
                        "StartedAt")),

            FinishedAt =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "FinishedAt"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal(
                            "FinishedAt")),

            DurationMs =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "DurationMs"))
                    ? null
                    : reader.GetInt64(
                        reader.GetOrdinal(
                            "DurationMs")),

            ErrorMessage =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "ErrorMessage"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal(
                            "ErrorMessage")),

            TotalItems =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "TotalItems"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal(
                            "TotalItems")),

            SucceededItems =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "SucceededItems"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal(
                            "SucceededItems")),

            FailedItems =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "FailedItems"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal(
                            "FailedItems")),

            AffectedRows =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "AffectedRows"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal(
                            "AffectedRows"))
        };
    }
    public async Task<PagedExecutionResultDto> GetPagedAsync(
       string? status,
       int? processId,
       DateTime? dateFrom,
       DateTime? dateTo,
       int pageNumber,
       int pageSize,
       string sortBy,
       string sortDirection,
       int staleThresholdMinutes,
       CancellationToken cancellationToken)
    {
        var executions = new List<ExecutionListItemDto>();

        var processes = new List<ExecutionProcessOptionDto>();

        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_ListPaged",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@Status", SqlDbType.VarChar, 20)
            .Value = string.IsNullOrWhiteSpace(status)
                ? DBNull.Value
                : status;

        command.Parameters
            .Add("@ProcessId", SqlDbType.Int)
            .Value = (object?)processId ?? DBNull.Value;

        command.Parameters
            .Add("@DateFrom", SqlDbType.Date)
            .Value = (object?)dateFrom?.Date ?? DBNull.Value;

        command.Parameters
            .Add("@DateTo", SqlDbType.Date)
            .Value = (object?)dateTo?.Date ?? DBNull.Value;

        command.Parameters
            .Add("@PageNumber", SqlDbType.Int)
            .Value = pageNumber;

        command.Parameters
            .Add("@PageSize", SqlDbType.Int)
            .Value = pageSize;
        /* INICIO AGREGADO: ordenamiento */

        command.Parameters
            .Add("@SortBy", SqlDbType.VarChar, 20)
            .Value = sortBy;

        command.Parameters
            .Add("@SortDirection", SqlDbType.VarChar, 4)
            .Value = sortDirection;

        /* FIN AGREGADO */

        command.Parameters
            .Add("@StaleThresholdMinutes", SqlDbType.Int)
            .Value = staleThresholdMinutes;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        // Grilla 1: ejecuciones de la página
        while (await reader.ReadAsync(cancellationToken))
        {
            executions.Add(
                new ExecutionListItemDto
                {
                    ExecutionId =
                        reader.GetInt64(
                            reader.GetOrdinal("ExecutionId")),

                    ProcessId =
                        reader.GetInt32(
                            reader.GetOrdinal("ProcessId")),

                    ProcessName =
                        reader.GetString(
                            reader.GetOrdinal("ProcessName")),

                    Status =
                        reader.GetString(
                            reader.GetOrdinal("Status")),

                    StartedAt =
                        reader.GetDateTime(
                            reader.GetOrdinal("StartedAt")),

                    FinishedAt =
                        reader.IsDBNull(
                            reader.GetOrdinal("FinishedAt"))
                            ? null
                            : reader.GetDateTime(
                                reader.GetOrdinal("FinishedAt")),

                    DurationMs =
                        reader.IsDBNull(
                            reader.GetOrdinal("DurationMs"))
                            ? null
                            : reader.GetInt64(
                                reader.GetOrdinal("DurationMs"))
                });
        }

        // Grilla 2: resumen y paginado
        if (!await reader.NextResultAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el resumen.");
        }

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "El resumen del paginado está vacío.");
        }

        int totalCount =
            reader.GetInt32(
                reader.GetOrdinal("TotalCount"));

        int returnedPageNumber =
            reader.GetInt32(
                reader.GetOrdinal("PageNumber"));

        int returnedPageSize =
            reader.GetInt32(
                reader.GetOrdinal("PageSize"));

        int totalPages =
            reader.GetInt32(
                reader.GetOrdinal("TotalPages"));

        int runningCount =
            reader.GetInt32(
                reader.GetOrdinal("RunningCount"));

        int staleRunningCount =
            reader.GetInt32(
                reader.GetOrdinal("StaleRunningCount"));

        int succeededCount =
            reader.GetInt32(
                reader.GetOrdinal("SucceededCount"));

        int failedCount =
            reader.GetInt32(
                reader.GetOrdinal("FailedCount"));

        long? averageDurationMs =
            reader.IsDBNull(
                reader.GetOrdinal("AverageDurationMs"))
                ? null
                : reader.GetInt64(
                    reader.GetOrdinal("AverageDurationMs"));

        // Grilla 3: procesos del combo
        if (!await reader.NextResultAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió los procesos.");
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            processes.Add(
                new ExecutionProcessOptionDto
                {
                    ProcessId =
                        reader.GetInt32(
                            reader.GetOrdinal("ProcessId")),

                    ProcessName =
                        reader.GetString(
                            reader.GetOrdinal("ProcessName"))
                });
        }

        return new PagedExecutionResultDto
        {
            Executions = executions,
            Processes = processes,
            TotalCount = totalCount,
            PageNumber = returnedPageNumber,
            PageSize = returnedPageSize,
            TotalPages = totalPages,
            RunningCount = runningCount,
            StaleRunningCount = staleRunningCount,
            SucceededCount = succeededCount,
            FailedCount = failedCount,
            AverageDurationMs = averageDurationMs
        };
    }

    public async Task<List<ExecutionListItemDto>> GetForExportAsync(
        string? status,
        int? processId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy,
        string sortDirection,
            int maxRows,
        CancellationToken cancellationToken)
    {
        var executions = new List<ExecutionListItemDto>();

        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_Export",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@Status", SqlDbType.VarChar, 20)
            .Value = string.IsNullOrWhiteSpace(status)
                ? DBNull.Value
                : status;

        command.Parameters
            .Add("@ProcessId", SqlDbType.Int)
            .Value = (object?)processId ?? DBNull.Value;

        command.Parameters
            .Add("@DateFrom", SqlDbType.Date)
            .Value = (object?)dateFrom?.Date ?? DBNull.Value;

        command.Parameters
            .Add("@DateTo", SqlDbType.Date)
            .Value = (object?)dateTo?.Date ?? DBNull.Value;

        command.Parameters
            .Add("@SortBy", SqlDbType.VarChar, 20)
            .Value = sortBy;

        command.Parameters
            .Add("@SortDirection", SqlDbType.VarChar, 4)
            .Value = sortDirection;
        command.Parameters
    .Add("@MaxRows", SqlDbType.Int)
    .Value = maxRows;
        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            executions.Add(
                new ExecutionListItemDto
                {
                    ExecutionId =
                        reader.GetInt64(
                            reader.GetOrdinal("ExecutionId")),

                    ProcessId =
                        reader.GetInt32(
                            reader.GetOrdinal("ProcessId")),

                    ProcessName =
                        reader.GetString(
                            reader.GetOrdinal("ProcessName")),

                    Status =
                        reader.GetString(
                            reader.GetOrdinal("Status")),

                    StartedAt =
                        reader.GetDateTime(
                            reader.GetOrdinal("StartedAt")),

                    FinishedAt =
                        reader.IsDBNull(
                            reader.GetOrdinal("FinishedAt"))
                            ? null
                            : reader.GetDateTime(
                                reader.GetOrdinal("FinishedAt")),

                    DurationMs =
                        reader.IsDBNull(
                            reader.GetOrdinal("DurationMs"))
                            ? null
                            : reader.GetInt64(
                                reader.GetOrdinal("DurationMs"))
                });
        }

        return executions;
    }

    public async Task<int> CloseStaleAsync(
        int maxAgeMinutes,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_CloseStale",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@MaxAgeMinutes", SqlDbType.Int)
            .Value = maxAgeMinutes;

        await connection.OpenAsync(cancellationToken);

        object? result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(result);
    }
    public async Task<List<StuckExecutionDto>> GetStuckAsync(
    int olderThanMinutes,
    CancellationToken cancellationToken)
    {
        var executions = new List<StuckExecutionDto>();

        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProcessExecution_ListStuck",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@OlderThanMinutes", SqlDbType.Int)
            .Value = olderThanMinutes;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            executions.Add(new StuckExecutionDto
            {
                ExecutionId =
                    reader.GetInt64(
                        reader.GetOrdinal("ExecutionId")),

                ProcessId =
                    reader.GetInt32(
                        reader.GetOrdinal("ProcessId")),

                ProcessName =
                    reader.GetString(
                        reader.GetOrdinal("ProcessName")),

                Status =
                    reader.GetString(
                        reader.GetOrdinal("Status")),

                StartedAt =
                    reader.GetDateTime(
                        reader.GetOrdinal("StartedAt")),

                RunningMinutes =
                    reader.GetInt64(
                        reader.GetOrdinal("RunningMinutes"))
            });
        }

        return executions;
    }
    public async Task CheckDatabaseConnectionAsync(
    CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);
    }

}