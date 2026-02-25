namespace VAH.Backend.Services;

/// <summary>
/// Abstraction for file storage operations.
/// Implementations: LocalStorageService (current), S3StorageService (future), AzureBlobStorageService (future).
/// </summary>
public interface IStorageService
{
    /// <summary>Upload a file and return the relative public path.</summary>
    Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType);

    /// <summary>Delete a file by its relative path.</summary>
    Task<bool> DeleteAsync(string filePath);

    /// <summary>Get the full public URL for a stored file.</summary>
    string GetPublicUrl(string filePath);

    /// <summary>Check if a file exists.</summary>
    bool Exists(string filePath);
}
