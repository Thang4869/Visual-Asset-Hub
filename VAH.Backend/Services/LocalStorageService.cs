namespace VAH.Backend.Services;

/// <summary>
/// Local filesystem storage implementation.
/// Stores files in wwwroot/uploads/ with GUID-based naming.
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
    {
        _uploadPath = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
        _logger = logger;

        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadPath, uniqueName);

        await using var outputStream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        _logger.LogInformation("File uploaded: {FileName} → {StoredName} ({Size} bytes)",
            originalFileName, uniqueName, outputStream.Length);

        return $"/uploads/{uniqueName}";
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return Task.FromResult(false);

        // Normalize: /uploads/filename.ext → full local path
        var relativePath = filePath.TrimStart('/');
        var fullPath = Path.Combine(
            Path.GetDirectoryName(_uploadPath)!, // go up to wwwroot
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);
            return Task.FromResult(true);
        }

        _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
        return Task.FromResult(false);
    }

    public string GetPublicUrl(string filePath)
    {
        return filePath; // Already a relative URL like /uploads/guid.ext
    }

    public bool Exists(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        var relativePath = filePath.TrimStart('/');
        var fullPath = Path.Combine(
            Path.GetDirectoryName(_uploadPath)!,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        return File.Exists(fullPath);
    }
}
