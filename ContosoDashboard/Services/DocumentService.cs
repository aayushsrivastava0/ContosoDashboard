using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed record DocumentUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string Title,
    string Category,
    int UserId,
    string? Description = null,
    int? ProjectId = null,
    int? TaskId = null,
    string? Tags = null);

public sealed record DocumentUploadResult(bool Succeeded, string Message, Document? Document = null);
public sealed record DocumentDownloadResult(Stream Content, string FileName, string ContentType);

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, CancellationToken cancellationToken = default);
    Task<List<Document>> GetAccessibleAsync(int userId, string? search = null, string? category = null, int? projectId = null);
    Task<Stream?> OpenAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<DocumentDownloadResult?> GetDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, int userId, string title, string category, string? description, string? tags, CancellationToken cancellationToken = default);
}

public sealed class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".png", ".jpg", ".jpeg", ".gif"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IScanQueueService _scanQueue;
    private readonly DocumentAccessService _access;
    private readonly INotificationService _notifications;
    private readonly DocumentStorageOptions _options;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IScanQueueService scanQueue,
        DocumentAccessService access, INotificationService notifications, IOptions<DocumentStorageOptions> options)
    {
        _context = context;
        _storage = storage;
        _scanQueue = scanQueue;
        _access = access;
        _notifications = notifications;
        _options = options.Value;
    }

    public async Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Category))
            return new(false, "Title and category are required.");
        if (request.Length <= 0 || request.Length > _options.MaxFileSizeBytes)
            return new(false, "Each document must be between 1 byte and 25 MB.");
        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension))
            return new(false, "This file type is not supported.");
        if (request.ProjectId.HasValue && !await _access.CanUseProjectAsync(request.ProjectId.Value, request.UserId))
            return new(false, "You are not authorized to add documents to this project.");

        StoredFileResult? stored = null;
        try
        {
            stored = await _storage.SaveAsync(request.Content, request.FileName, cancellationToken);
            var document = new Document
            {
                Title = request.Title.Trim(),
                Category = request.Category.Trim(),
                Description = request.Description?.Trim(),
                FileName = Path.GetFileName(request.FileName),
                StoredFileName = stored.StoredFileName,
                FilePath = stored.RelativePath,
                FileType = request.ContentType,
                FileSizeBytes = request.Length,
                UploadedByUserId = request.UserId,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                Tags = request.Tags?.Trim(),
                ScanStatus = DocumentScanStatus.Pending
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            _context.AuditLogs.Add(new AuditLog { DocumentId = document.DocumentId, UserId = request.UserId, ActionType = "Upload", Details = "Upload accepted and queued for local scan." });
            await _context.SaveChangesAsync(cancellationToken);
            await _scanQueue.EnqueueAsync(new DocumentScanMessage(document.DocumentId, document.FilePath, document.FileName, document.FileType), cancellationToken);
            return new(true, "Upload accepted. The document is pending a local security scan.", document);
        }
        catch
        {
            if (stored is not null) await _storage.DeleteAsync(stored.RelativePath, cancellationToken);
            return new(false, "The document could not be saved. Please try again.");
        }
    }

    public async Task<List<Document>> GetAccessibleAsync(int userId, string? search = null, string? category = null, int? projectId = null)
    {
        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => d.ScanStatus == DocumentScanStatus.Clean)
            .Where(d => d.UploadedByUserId == userId ||
                d.Shares.Any(s => s.UserId == userId && s.IsActive) ||
                (d.ProjectId.HasValue && _context.Projects.Any(p => p.ProjectId == d.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)))));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Title.Contains(search) || (d.Description != null && d.Description.Contains(search)) || (d.Tags != null && d.Tags.Contains(search)) || d.FileName.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId);
        return await query.OrderByDescending(d => d.UploadedDate).Take(500).ToListAsync();
    }

    public async Task<Stream?> OpenAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || document.ScanStatus != DocumentScanStatus.Clean || !await _access.CanAccessAsync(document, userId)) return null;
        _context.AuditLogs.Add(new AuditLog { DocumentId = documentId, UserId = userId, ActionType = "Download" });
        await _context.SaveChangesAsync(cancellationToken);
        return await _storage.OpenReadAsync(document.FilePath, cancellationToken);
    }

    public async Task<DocumentDownloadResult?> GetDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || document.ScanStatus != DocumentScanStatus.Clean || !await _access.CanAccessAsync(document, userId)) return null;
        var content = await _storage.OpenReadAsync(document.FilePath, cancellationToken);
        return content is null ? null : new DocumentDownloadResult(content, document.FileName, document.FileType);
    }

    public async Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || !await _access.CanManageAsync(document, userId)) return false;
        await _storage.DeleteAsync(document.FilePath, cancellationToken);
        _context.AuditLogs.Add(new AuditLog { DocumentId = documentId, UserId = userId, ActionType = "Delete" });
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || document.ScanStatus != DocumentScanStatus.Clean || !await _access.CanManageAsync(document, requestingUserId)) return false;
        if (await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.UserId == recipientUserId && s.IsActive, cancellationToken)) return false;
        _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, UserId = recipientUserId, SharedByUserId = requestingUserId });
        document.IsShared = true;
        _context.AuditLogs.Add(new AuditLog { DocumentId = documentId, UserId = requestingUserId, ActionType = "Share", Details = $"Shared with user {recipientUserId}." });
        await _context.SaveChangesAsync(cancellationToken);
        await _notifications.CreateNotificationAsync(new Notification { UserId = recipientUserId, Title = "Document shared", Message = $"{document.Title} was shared with you.", Type = NotificationType.SystemAnnouncement, Priority = NotificationPriority.Informational });
        return true;
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int userId, string title, string category, string? description, string? tags, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || !await _access.CanManageAsync(document, userId) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category)) return false;
        document.Title = title.Trim();
        document.Category = category.Trim();
        document.Description = description?.Trim();
        document.Tags = tags?.Trim();
        document.UpdatedDate = DateTime.UtcNow;
        _context.AuditLogs.Add(new AuditLog { DocumentId = documentId, UserId = userId, ActionType = "Edit", Details = "Document metadata updated." });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
