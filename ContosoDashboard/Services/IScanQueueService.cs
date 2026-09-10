using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentScanMessage(int DocumentId, string RelativePath, string FileName, string FileType);

public interface IScanQueueService
{
    Task EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
