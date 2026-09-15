using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;

namespace SystemDesign.CloudStorage.Infrastructure.Storage;

/// <summary>
/// The result of a streaming file write.
/// </summary>
internal sealed record WrittenFile(string RelativePath, long Length, string Sha256, string ETag);

/// <summary>
/// Filesystem blob storage that never uses logical keys in physical paths.
/// </summary>
internal sealed class BlobStorage
{
    private readonly StorageOptions _options;
    
    public BlobStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        
        Directory.CreateDirectory(Objects);
        Directory.CreateDirectory(Temporary);
        Directory.CreateDirectory(Multipart);
    }

    private string Objects => 
        
        Path.Combine(_options.RootPath, "objects");
    
    private string Temporary => 
        Path.Combine(_options.RootPath, "temporary");
    
    private string Multipart => 
        Path.Combine(_options.RootPath, "multipart");

    /// <summary>
    /// Validates and normalizes a server-generated relative path.
    /// </summary>
    public string FullPath(string relative)
    {
        var root = Path.GetFullPath(_options.RootPath) + Path.DirectorySeparatorChar;
        
        var full = Path.GetFullPath(Path.Combine(_options.RootPath, relative));

        if (!full.StartsWith(root, StringComparison.Ordinal))
            throw new InvalidOperationException($"Invalid internal path '{full}'. Expected path to be within root '{root}'.");

        return full;
    }

    /// <summary>
    /// Streams input to a temporary file while computing SHA-256.
    /// </summary>
    public async Task<WrittenFile> WriteTemporaryAsync(Stream input, long maxBytes, string area, CancellationToken ct)
    {
        var dir = area == "multipart" ? Multipart : Temporary;
        var name = $"{Guid.NewGuid():N}.tmp";
        var path = Path.Combine(dir, name);
        
        long total = 0;
        
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        try
        {
            await using (var output = new FileStream(
                             path,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             128 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[128 * 1024];
                
                int read;
                while ((read = await input.ReadAsync(buffer, ct)) > 0)
                {
                    total += read;

                    if (total > maxBytes)
                        throw new AppException(413, "payload_too_large", $"Maximum allowed size of {maxBytes} bytes exceeded.");

                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }

                await output.FlushAsync(ct);
                output.Flush(true);
            }
            
            var sha = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            
            return new(Path.GetRelativePath(_options.RootPath, path), total, sha, $"\"{sha}\"");
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    /// <summary>
    /// Atomically promotes a temporary file into a fan-out directory.
    /// </summary>
    public WrittenFile Promote(WrittenFile temp, Guid id)
    {
        var n = id.ToString("N");
        var relative = Path.Combine("objects", n[..2], n.Substring(2, 2), n + ".blob");
        var final = FullPath(relative);
        
        Directory.CreateDirectory(Path.GetDirectoryName(final)!);
        
        File.Move(FullPath(temp.RelativePath), final);
        
        return temp with { RelativePath = relative };
    }

    /// <summary>
    /// Opens an asynchronous read-only stream.
    /// </summary>
    public Stream OpenRead(string relative) => 
        new FileStream(FullPath(relative),
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       128 * 1024,
                       FileOptions.Asynchronous | FileOptions.SequentialScan);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    public void Delete(string relative) => File.Delete(FullPath(relative));

    /// <summary>
    /// Enumerates files in temporary storage areas.
    /// </summary>
    public IEnumerable<string> EnumerateTemporary() =>
        Directory.EnumerateFiles(Temporary, "*.tmp").Concat(Directory.EnumerateFiles(Multipart, "*.tmp"));

    /// <summary>
    /// The storage root directory.
    /// </summary>
    public string RootPath => _options.RootPath;
}
