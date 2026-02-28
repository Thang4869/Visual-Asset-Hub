namespace VAH.Backend.Models;

/// <summary>
/// Paginated result wrapper for list endpoints.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
    public bool HasPreviousPage => Page > 1;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Common query parameters for paginated list endpoints.
/// Validated via Data Annotations — <see cref="System.ComponentModel.DataAnnotations.RangeAttribute"/>.
/// </summary>
public class PaginationParams
{
    /// <summary>Page number (1-based).</summary>
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Page must be ≥ 1.")]
    public int Page { get; set; } = 1;

    /// <summary>Items per page (1–100).</summary>
    [System.ComponentModel.DataAnnotations.Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 50;

    /// <summary>Optional sort field (e.g. "filename", "createdat").</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: "asc" or "desc".</summary>
    public string SortOrder { get; set; } = "asc";
}

/// <summary>
/// Configuration for file upload restrictions.
/// </summary>
public class FileUploadConfig
{
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB
    public int MaxFilesPerRequest { get; set; } = 20;
    public string[] AllowedExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".ico",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".md", ".json", ".csv",
        ".mp4", ".webm", ".mp3", ".wav",
        ".zip", ".rar", ".7z"
    };
    public string[] AllowedMimeTypePrefixes { get; set; } =
    {
        "image/", "video/", "audio/", "application/pdf",
        "application/msword", "application/vnd.", "text/", "application/json",
        "application/zip", "application/x-rar", "application/x-7z"
    };
}
