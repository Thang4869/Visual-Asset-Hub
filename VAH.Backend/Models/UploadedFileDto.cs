using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Models;

/// <summary>
/// Represents an uploaded file in a transport-agnostic way so higher layers remain free of ASP.NET Core types.
/// </summary>
public sealed class UploadedFileDto : IUploadedFile
{
    /// <summary>
    /// Creates a synchronous-upload descriptor.
    /// <para>The <see cref="OpenStream"/> factory returns a <see cref="Stream"/> that the caller MUST dispose when finished.</para>
    /// </summary>
    public UploadedFileDto(string fileName, string? contentType, long length, Func<Stream> openStream)
    {
        FileName = ValidateFileName(fileName);
        ContentType = contentType;
        Length = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        OpenStream = openStream ?? throw new ArgumentNullException(nameof(openStream));
        OpenStreamAsync = null;
    }

    /// <summary>
    /// Creates an asynchronous-upload descriptor.
    /// <para>The <see cref="OpenStreamAsync"/> factory returns a <see cref="Stream"/> that the caller MUST dispose when finished.</para>
    /// </summary>
    public UploadedFileDto(string fileName, string? contentType, long length, Func<CancellationToken, Task<Stream>> openStreamAsync)
    {
        FileName = ValidateFileName(fileName);
        ContentType = contentType;
        Length = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        OpenStreamAsync = openStreamAsync ?? throw new ArgumentNullException(nameof(openStreamAsync));
        OpenStream = null;
    }

    /// <summary>
    /// File name as provided by the uploader.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Optional content type (MIME) provided by the uploader.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Length in bytes. Guaranteed to be non-negative.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Factory that opens a synchronous <see cref="Stream"/> for reading the uploaded content. May be <c>null</c> if only <see cref="OpenStreamAsync"/> is available.
    /// The returned stream MUST be disposed by the caller.
    /// </summary>
    public Func<Stream>? OpenStream { get; }

    /// <summary>
    /// Factory that opens an asynchronous <see cref="Stream"/> for reading the uploaded content. May be <c>null</c> if only <see cref="OpenStream"/> is available.
    /// The returned stream MUST be disposed by the caller.
    /// </summary>
    public Func<CancellationToken, Task<Stream>>? OpenStreamAsync { get; }

    /// <summary>
    /// Returns a metadata-only DTO suitable for serialization across boundaries.
    /// </summary>
    public UploadedFileMetadataDto CreateMetadata()
    {
        return new UploadedFileMetadataDto
        {
            FileName = FileName,
            ContentType = ContentType,
            Length = Length,
            HasSyncStream = OpenStream != null,
            HasAsyncStream = OpenStreamAsync != null
        };
    }

    /// <summary>
    /// Validates that <see cref="Length"/> equals the actual bytes available from the stream factory.
    /// The returned stream is disposed by this method; callers should not assume the stream remains open.
    /// </summary>
    public async Task<bool> ValidateLengthAsync(CancellationToken cancellationToken = default)
    {
        if (Length < 0)
            return false;

        if (OpenStreamAsync != null)
        {
            await using var s = await OpenStreamAsync(cancellationToken).ConfigureAwait(false);
            return await CompareStreamLengthAsync(s, Length, cancellationToken).ConfigureAwait(false);
        }

        if (OpenStream != null)
        {
            using var s = OpenStream();
            return CompareStreamLength(s, Length, cancellationToken);
        }

        throw new InvalidOperationException("No stream factory available to validate length.");
    }

    private static bool CompareStreamLength(Stream s, long expected, CancellationToken cancellationToken)
    {
        if (s.CanSeek)
        {
            return s.Length == expected;
        }

        Span<byte> buffer = stackalloc byte[8192];
        long total = 0;
        int read;
        while ((read = s.Read(buffer)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += read;
            if (total > expected)
                return false;
        }

        return total == expected;
    }

    private static async Task<bool> CompareStreamLengthAsync(Stream s, long expected, CancellationToken cancellationToken)
    {
        if (s.CanSeek)
        {
            return s.Length == expected;
        }

        var buffer = new byte[8192];
        long total = 0;
        int read;
        while ((read = await s.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > expected)
                return false;
        }

        return total == expected;
    }

    private static string ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (fileName.Length > 260)
            throw new ArgumentException("File name is too long.", nameof(fileName));

        // Disallow path separators and invalid file name characters to avoid path traversal or surprises.
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));

        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("File name must not contain directory separators.", nameof(fileName));

        return fileName;
    }
}
