namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public sealed record StoredFileResult(string StoredFileName, string RelativePath);
