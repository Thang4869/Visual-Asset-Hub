using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace VAH.Backend.Services;

/// <summary>
/// Generates multi-size thumbnails for uploaded images using ImageSharp.
/// Output format: WebP (best compression + quality balance).
/// </summary>
public class ThumbnailService : IThumbnailService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ThumbnailService> _logger;

    private static readonly Dictionary<string, int> Sizes = new()
    {
        ["sm"] = 150,  // grid thumbnail
        ["md"] = 400,  // preview panel
        ["lg"] = 800,  // large preview
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    public ThumbnailService(IWebHostEnvironment env, ILogger<ThumbnailService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GenerateThumbnailsAsync(string originalFilePath)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(originalFilePath))
            return result;

        // Check extension
        var ext = Path.GetExtension(originalFilePath);
        if (!SupportedExtensions.Contains(ext))
            return result;

        // Resolve full path
        var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relativePath = originalFilePath.TrimStart('/');
        var fullPath = Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Original file not found for thumbnail: {Path}", originalFilePath);
            return result;
        }

        // Create thumbs directory
        var thumbsDir = Path.Combine(wwwroot, "uploads", "thumbs");
        Directory.CreateDirectory(thumbsDir);

        var fileId = Path.GetFileNameWithoutExtension(fullPath);

        try
        {
            using var image = await Image.LoadAsync(fullPath);
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            foreach (var (label, maxDim) in Sizes)
            {
                // Skip if original is smaller than this size
                if (originalWidth <= maxDim && originalHeight <= maxDim)
                {
                    // Use original as the thumbnail for this size
                    result[label] = originalFilePath;
                    continue;
                }

                var thumbFileName = $"{label}_{fileId}.webp";
                var thumbFullPath = Path.Combine(thumbsDir, thumbFileName);
                var thumbRelativeUrl = $"/uploads/thumbs/{thumbFileName}";

                // Clone and resize
                using var clone = image.Clone(ctx =>
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxDim, maxDim),
                    }));

                await clone.SaveAsWebpAsync(thumbFullPath, new WebpEncoder
                {
                    Quality = 80,
                    FileFormat = WebpFileFormatType.Lossy,
                });

                result[label] = thumbRelativeUrl;
                _logger.LogDebug("Thumbnail generated: {Label} → {Path} ({W}×{H})",
                    label, thumbRelativeUrl, clone.Width, clone.Height);
            }

            _logger.LogInformation("Thumbnails generated for {File}: {Sizes}",
                originalFilePath, string.Join(", ", result.Keys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnails for {File}", originalFilePath);
        }

        return result;
    }
}
