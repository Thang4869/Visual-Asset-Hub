using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Models;

/// <summary>
/// Abstraction for an uploaded file descriptor to aid testability and extension.
/// </summary>
public interface IUploadedFile
{
    string FileName { get; }
    string? ContentType { get; }
    long Length { get; }
    Func<Stream>? OpenStream { get; }
    Func<CancellationToken, Task<Stream>>? OpenStreamAsync { get; }

    /// <summary>
    /// Returns a serializable metadata-only DTO suitable for transport/serialization boundaries.
    /// </summary>
    UploadedFileMetadataDto CreateMetadata();

    /// <summary>
    /// Optionally validates that the provided <see cref="Length"/> matches the actual bytes available from the stream factory.
    /// Implementations that cannot check should throw <see cref="InvalidOperationException"/>.
    /// </summary>
    Task<bool> ValidateLengthAsync(CancellationToken cancellationToken = default);
}
