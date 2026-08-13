using Microsoft.Data.SqlClient;

namespace SqlWorkflowMonitor.Data;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("WorkflowMonitor")
            ?? throw new InvalidOperationException(
                "No se encontró la connection string 'WorkflowMonitor'.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}