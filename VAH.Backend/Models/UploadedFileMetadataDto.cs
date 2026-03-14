namespace VAH.Backend.Models;

/// <summary>
/// Serializable metadata-only DTO for uploaded files. Does not include stream content.
/// </summary>
public sealed class UploadedFileMetadataDto
{
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public long Length { get; init; }
    public bool HasSyncStream { get; init; }
    public bool HasAsyncStream { get; init; }
}
