namespace SqlWorkflowMonitor.Services;

public sealed class ProductAccessDeniedException : Exception
{
    public ProductAccessDeniedException(string message)
        : base(message)
    {
    }
}
