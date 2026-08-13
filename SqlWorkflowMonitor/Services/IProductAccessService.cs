using SqlWorkflowMonitor.Models;

namespace SqlWorkflowMonitor.Services;

public interface IProductAccessService
{
    Task<ProductAccessStatus> GetStatusAsync(
        CancellationToken cancellationToken);

    Task ValidateCanStartAsync(
        int processId,
        string workerId,
        CancellationToken cancellationToken);
}
