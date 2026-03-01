using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Infrastructure.Files;

public interface IFileMapperService
{
    IReadOnlyCollection<UploadedFileDto> Map(IReadOnlyCollection<IFormFile> files);
}
