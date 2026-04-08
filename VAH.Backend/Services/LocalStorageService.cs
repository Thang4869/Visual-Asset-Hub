#pragma warning disable
#pragma warning disable CA2022, CS1998
#pragma warning disable CA2022, CS1998
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

    public async Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default)
    {
        if (fileStream.CanSeek && fileStream.Length >= 2)
        {
            var header = new byte[2];
            await fileStream.ReadAsync(header, 0, 2, ct);
            if (header[0] == 0x4D && header[1] == 0x5A) // 'M', 'Z'
            {
                throw new System.Security.SecurityException("Executable files disguised as safe assets are prohibited.");
            }
            fileStream.Position = 0;
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadPath, uniqueName);

        await using var outputStream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream, ct);

        _logger.LogInformation("File uploaded: {FileName} → {StoredName} ({Size} bytes)",
            originalFileName, uniqueName, outputStream.Length);

        return $"/uploads/{uniqueName}";
    }

        private string GetSecureFullPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
        var relativePath = filePath.TrimStart('/');
        // Assuming filePath is always prefixed with /uploads/
        var wwwrootDir = Path.GetDirectoryName(_uploadPath)!;
        var fullPath = Path.GetFullPath(Path.Combine(wwwrootDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        
        // Anti-directory traversal check
        if (!fullPath.StartsWith(Path.GetFullPath(wwwrootDir), StringComparison.OrdinalIgnoreCase))
            throw new System.Security.SecurityException("Path traversal attempt detected: " + filePath);
            
        return fullPath;
    }

    public Task<bool> DeleteAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return Task.FromResult(false);

        var fullPath = GetSecureFullPath(filePath);

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
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        var fullPath = GetSecureFullPath(filePath);
        return File.Exists(fullPath);
    }
}





