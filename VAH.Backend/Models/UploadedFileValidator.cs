using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Models;

public sealed class UploadedFileValidator : IUploadedFileValidator
{
    private const int BufferSize = 81920; // 80 KB recommended for large streams

    public async Task<bool> ValidateLengthAsync(IUploadedFile file, CancellationToken cancellationToken = default)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Length < 0) return false;

        if (file.OpenStreamAsync != null)
        {
            await using var s = await file.OpenStreamAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("OpenStreamAsync returned null stream.");
            return await CompareStreamLengthAsync(s, file.Length, cancellationToken).ConfigureAwait(false);
        }

        if (file.OpenStream != null)
        {
            using var s = file.OpenStream() ?? throw new InvalidOperationException("OpenStream returned null stream.");
            return CompareStreamLength(s, file.Length, cancellationToken);
        }

        throw new InvalidOperationException("No stream factory available to validate length.");
    }

    private static bool CompareStreamLength(Stream s, long expected, CancellationToken cancellationToken)
    {
        if (s.CanSeek)
        {
            return s.Length == expected;
        }

        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(BufferSize);
        try
        {
            long total = 0;
            int read;
            while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                total += read;
                if (total > expected) return false;
            }

            return total == expected;
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    private static async Task<bool> CompareStreamLengthAsync(Stream s, long expected, CancellationToken cancellationToken)
    {
        if (s.CanSeek)
        {
            return s.Length == expected;
        }

        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(BufferSize);
        try
        {
            long total = 0;
            int read;
            while ((read = await s.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > expected) return false;
            }

            return total == expected;
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}
