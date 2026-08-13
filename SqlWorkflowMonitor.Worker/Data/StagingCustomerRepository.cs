using System.Data;
using Microsoft.Data.SqlClient;
using SqlWorkflowMonitor.Worker.Models;

namespace SqlWorkflowMonitor.Worker.Data;

public sealed class StagingCustomerRepository
{
    private readonly string _connectionString;

    public StagingCustomerRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("WorkflowMonitor")
            ?? throw new InvalidOperationException(
                "No se encontró la connection string 'WorkflowMonitor'.");
    }

    public async Task<int> InsertAndProcessAsync(
        long executionId,
        IReadOnlyCollection<StagingCustomer> customers,
        CancellationToken cancellationToken)
    {
        if (customers.Count == 0)
        {
            throw new InvalidOperationException(
                "No hay clientes para procesar.");
        }

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await InsertStagingAsync(
            connection,
            executionId,
            customers,
            cancellationToken);

        return await ProcessStagingAsync(
            connection,
            executionId,
            cancellationToken);
    }

    private static async Task InsertStagingAsync(
        SqlConnection connection,
        long executionId,
        IReadOnlyCollection<StagingCustomer> customers,
        CancellationToken cancellationToken)
    {
        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO dbo.StagingCustomers
                (
                    ExecutionId,
                    Name,
                    Email,
                    IsValid,
                    ValidationError
                )
                VALUES
                (
                    @ExecutionId,
                    @Name,
                    @Email,
                    @IsValid,
                    @ValidationError
                );
                """;

            using var command =
                new SqlCommand(sql, connection, transaction);

            SqlParameter executionParameter =
                command.Parameters.Add(
                    "@ExecutionId",
                    SqlDbType.BigInt);

            SqlParameter nameParameter =
                command.Parameters.Add(
                    "@Name",
                    SqlDbType.NVarChar,
                    150);

            SqlParameter emailParameter =
                command.Parameters.Add(
                    "@Email",
                    SqlDbType.NVarChar,
                    200);

            SqlParameter validParameter =
                command.Parameters.Add(
                    "@IsValid",
                    SqlDbType.Bit);

            SqlParameter errorParameter =
                command.Parameters.Add(
                    "@ValidationError",
                    SqlDbType.NVarChar,
                    500);

            foreach (StagingCustomer customer in customers)
            {
                executionParameter.Value = executionId;

                nameParameter.Value =
                    (object?)customer.Name ?? DBNull.Value;

                emailParameter.Value =
                    (object?)customer.Email ?? DBNull.Value;

                validParameter.Value = customer.IsValid;

                errorParameter.Value =
                    (object?)customer.ValidationError
                    ?? DBNull.Value;

                await command.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static async Task<int> ProcessStagingAsync(
        SqlConnection connection,
        long executionId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            "dbo.sp_StagingCustomers_Process",
            connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters
            .Add("@ExecutionId", SqlDbType.BigInt)
            .Value = executionId;

        object? result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(result);
    }
}