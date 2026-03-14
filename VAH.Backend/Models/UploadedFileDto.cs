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
    private const int MaxFileNameLength = 260;
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

    // Validation logic has been moved to a dedicated validator (IUploadedFileValidator / UploadedFileValidator)

    private static string ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (fileName.Length > MaxFileNameLength)
            throw new ArgumentException("File name is too long.", nameof(fileName));

        // Disallow path separators and invalid file name characters to avoid path traversal or surprises.
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));

        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("File name must not contain directory separators.", nameof(fileName));

        return fileName;
    }
}
