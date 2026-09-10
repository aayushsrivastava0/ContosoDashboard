using ContosoDashboard.Services;

namespace ContosoDashboard.Background;

// Azure deployment adapter: bind the same DocumentScanMessage to an Azure Queue trigger when cloud infrastructure is enabled.
public sealed class DocumentVirusScanFunction
{
    private readonly IScanQueueService _scanQueue;

    public DocumentVirusScanFunction(IScanQueueService scanQueue)
    {
        _scanQueue = scanQueue;
    }

    public Task ProcessAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        return _scanQueue.EnqueueAsync(message, cancellationToken);
    }
}
