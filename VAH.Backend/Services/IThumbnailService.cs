namespace VAH.Backend.Services;

/// <summary>
/// Generates thumbnails for uploaded images.
/// Sizes: small (150px), medium (400px), large (800px).
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Generate thumbnails for an image file.
    /// Returns dictionary of size-label → relative URL (e.g. "/uploads/thumbs/sm_guid.webp").
    /// Returns empty dict if file is not an image or processing fails.
    /// </summary>
    Task<Dictionary<string, string>> GenerateThumbnailsAsync(string originalFilePath);
}
