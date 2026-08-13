namespace SqlWorkflowMonitor.Models;

public sealed class ProductAccessStatus
{
    public string Edition { get; init; } = "Demo";

    public DateTime InstalledAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public bool IsExpired { get; init; }
    // INICIO AGREGADO - Estado detallado del acceso
    public bool IsDemo { get; init; } = true;

    public bool IsInGracePeriod { get; init; }

    public bool IsInvalidLicense { get; init; }

    public bool IsExpiringSoon { get; init; }

    public string? AccessError { get; init; }
    // FIN AGREGADO

    public int DaysRemaining { get; init; }

    public int MaxProcesses { get; init; }

    public int RegisteredProcesses { get; init; }

    public int MaxWorkers { get; init; }

    public int RegisteredWorkers { get; init; }

    public bool CsvExportEnabled { get; init; }

    public bool CanStartExecutions => !IsExpired;
}
