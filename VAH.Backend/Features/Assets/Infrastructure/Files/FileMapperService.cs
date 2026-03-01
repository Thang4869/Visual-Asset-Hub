using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Infrastructure.Files;

internal sealed class FileMapperService : IFileMapperService
{
    public IReadOnlyCollection<UploadedFileDto> Map(IReadOnlyCollection<IFormFile> files)
    {
        if (files is null || files.Count == 0)
        {
            return Array.Empty<UploadedFileDto>();
        }

        return files
            .Where(file => file.Length > 0)
            .Select(file => new UploadedFileDto(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream))
            .ToArray();
    }
}
