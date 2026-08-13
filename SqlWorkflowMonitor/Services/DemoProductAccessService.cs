using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Models;

namespace SqlWorkflowMonitor.Services;

public sealed class DemoProductAccessService
    : IProductAccessService
{
    private readonly ProductAccessRepository _repository;

    public DemoProductAccessService(
        ProductAccessRepository repository)
    {
        _repository = repository;
    }

    public Task<ProductAccessStatus> GetStatusAsync(
        CancellationToken cancellationToken)
    {
        return _repository.GetStatusAsync(cancellationToken);
    }

    public Task ValidateCanStartAsync(
        int processId,
        string workerId,
        CancellationToken cancellationToken)
    {
        return _repository.ValidateAndRegisterStartAsync(
            processId,
            workerId,
            cancellationToken);
    }
}
