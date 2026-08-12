using System.Data;
using Microsoft.Data.SqlClient;
using SqlWorkflowMonitor.Models;
using SqlWorkflowMonitor.Licensing.Models;
namespace SqlWorkflowMonitor.Data;

public sealed class ProductAccessRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ProductAccessRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProductAccessStatus> GetStatusAsync(
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProductAccess_GetDemoStatus",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "No se pudo obtener el estado de la Demo.");
        }

        DateTime installedAtUtc = DateTime.SpecifyKind(
            reader.GetDateTime(
                reader.GetOrdinal("InstalledAtUtc")),
            DateTimeKind.Utc);

        DateTime expiresAtUtc = DateTime.SpecifyKind(
            reader.GetDateTime(
                reader.GetOrdinal("ExpiresAtUtc")),
            DateTimeKind.Utc);

        DateTime currentUtc = DateTime.SpecifyKind(
            reader.GetDateTime(
                reader.GetOrdinal("CurrentUtc")),
            DateTimeKind.Utc);

        bool isExpired = currentUtc >= expiresAtUtc;

        int daysRemaining = isExpired
            ? 0
            : Math.Max(
                1,
                (int)Math.Ceiling(
                    (expiresAtUtc - currentUtc).TotalDays));

        return new ProductAccessStatus
        {
            Edition = "Demo",
            InstalledAtUtc = installedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            IsExpired = isExpired,
            DaysRemaining = daysRemaining,
            MaxProcesses = reader.GetInt32(
                reader.GetOrdinal("MaxProcesses")),
            RegisteredProcesses = reader.GetInt32(
                reader.GetOrdinal("RegisteredProcesses")),
            MaxWorkers = reader.GetInt32(
                reader.GetOrdinal("MaxWorkers")),
            RegisteredWorkers = reader.GetInt32(
                reader.GetOrdinal("RegisteredWorkers")),
            CsvExportEnabled = reader.GetBoolean(
                reader.GetOrdinal("CsvExportEnabled"))
        };
    }

    public async Task ValidateAndRegisterStartAsync(
        int processId,
        string workerId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProductAccess_ValidateDemoStart",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters
            .Add("@ProcessId", SqlDbType.Int)
            .Value = processId;

        command.Parameters
            .Add("@WorkerId", SqlDbType.NVarChar, 100)
            .Value = workerId;

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task<LicenseInstallationContext>
    GetLicenseInstallationContextAsync(
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProductAccess_GetLicenseContext",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync(cancellationToken);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "No se encontró la instalación del producto.");
        }

        int installationIdOrdinal =
            reader.GetOrdinal("LicenseInstallationId");

        if (reader.IsDBNull(installationIdOrdinal))
        {
            throw new InvalidOperationException(
                "La instalación no tiene un LicenseInstallationId.");
        }

        DateTime currentUtc = DateTime.SpecifyKind(
            reader.GetDateTime(
                reader.GetOrdinal("CurrentUtc")),
            DateTimeKind.Utc);

        return new LicenseInstallationContext
        {
            InstallationId =
                reader.GetGuid(installationIdOrdinal),

            CurrentUtc =
                new DateTimeOffset(currentUtc)
        };
    }
    public async Task ValidateAndRegisterStartAsync(
        int processId,
        string workerId,
        int maxProcesses,
        int maxWorkers,
        DateTimeOffset accessExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            _connectionFactory.CreateConnection();

        using var command = new SqlCommand(
            "dbo.sp_ProductAccess_ValidateAndRegisterStart",
            connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@ProcessId",
            SqlDbType.Int).Value = processId;

        command.Parameters.Add(
            "@WorkerId",
            SqlDbType.NVarChar,
            100).Value = workerId;

        command.Parameters.Add(
            "@MaxProcesses",
            SqlDbType.Int).Value = maxProcesses;

        command.Parameters.Add(
            "@MaxWorkers",
            SqlDbType.Int).Value = maxWorkers;

        command.Parameters.Add(
            "@AccessExpiresAtUtc",
            SqlDbType.DateTime2).Value =
                accessExpiresAtUtc.UtcDateTime;

        await connection.OpenAsync(
            cancellationToken);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }
}
