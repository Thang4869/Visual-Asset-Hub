using System;
using System.IO;

namespace VAH.Backend.Models;

/// <summary>
/// Represents an uploaded file in a transport-agnostic way so higher layers remain free of ASP.NET Core types.
/// </summary>
public sealed class UploadedFileDto
{
    public UploadedFileDto(string fileName, string? contentType, long length, Func<Stream> openStream)
    {
        FileName = string.IsNullOrWhiteSpace(fileName)
            ? throw new ArgumentException("File name is required.", nameof(fileName))
            : fileName;
        ContentType = contentType;
        Length = length;
        OpenStream = openStream ?? throw new ArgumentNullException(nameof(openStream));
    }

    public string FileName { get; }
    public string? ContentType { get; }
    public long Length { get; }
    public Func<Stream> OpenStream { get; }
}
