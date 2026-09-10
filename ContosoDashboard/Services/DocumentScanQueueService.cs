using System.Collections.Concurrent;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentScanQueueService : IScanQueueService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentQueue<DocumentScanMessage> _queue = new();

    public DocumentScanQueueService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        while (_queue.TryDequeue(out var message))
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var document = await context.Documents.FirstOrDefaultAsync(d => d.DocumentId == message.DocumentId, cancellationToken);
            if (document is null) continue;

            document.ScanStatus = message.FileName.Contains("eicar", StringComparison.OrdinalIgnoreCase)
                ? DocumentScanStatus.Quarantined
                : DocumentScanStatus.Clean;
            document.ScanCompletedDate = DateTime.UtcNow;
            context.AuditLogs.Add(new AuditLog
            {
                DocumentId = document.DocumentId,
                UserId = document.UploadedByUserId,
                ActionType = "ScanCompleted",
                Details = $"Local mock scan result: {document.ScanStatus}"
            });
            await context.SaveChangesAsync(cancellationToken);
            await notifications.CreateNotificationAsync(new Notification
            {
                UserId = document.UploadedByUserId,
                Title = document.ScanStatus == DocumentScanStatus.Clean ? "Document ready" : "Document blocked",
                Message = document.ScanStatus == DocumentScanStatus.Clean
                    ? $"{document.Title} passed the local virus scan."
                    : $"{document.Title} was blocked by the local virus scan.",
                Type = NotificationType.SystemAnnouncement,
                Priority = document.ScanStatus == DocumentScanStatus.Clean
                    ? NotificationPriority.Informational
                    : NotificationPriority.Important
            });
        }
    }
}
