using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Models;

public interface IUploadedFileValidator
{
    Task<bool> ValidateLengthAsync(IUploadedFile file, CancellationToken cancellationToken = default);
}
