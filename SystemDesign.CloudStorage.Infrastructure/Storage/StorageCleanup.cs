using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace SystemDesign.CloudStorage.Infrastructure.Storage;

/// <summary>
/// Deletes only sufficiently old temporary files and confirmed orphan blobs.
/// </summary>
internal sealed class StorageCleanup(CloudStorageDbContext db,
                                     BlobStorage files,
                                     IOptions<StorageOptions> options,
                                     ILogger<StorageCleanup> logger) : IStorageCleanup
{
    /// <inheritdoc />
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = await db.MultipartUploads.Include(x => x.Parts)
                          .Where(x => x.Status == MultipartStatus.Active && x.ExpiresAtUtc < now)
                          .ToListAsync(ct);
        
        foreach (var u in expired)
        {
            foreach (var p in u.Parts)
                files.Delete(p.RelativePath);
            
            u.Status = MultipartStatus.Aborted;
        }
        
        var referenced =
            await db.Blobs.Where(b => db.ObjectVersions.Any(v => v.BlobId == b.Id)).Select(x => x.Id).ToListAsync(ct);
        
        var orphans =
            await db.Blobs
                .Where(x => !referenced.Contains(x.Id) && x.CreatedAtUtc < now - options.Value.TemporaryFileLifetime)
                .ToListAsync(ct);
        
        foreach (var b in orphans)
        {
            files.Delete(b.RelativePath);
            db.Remove(b);
        }
        
        var threshold = now - options.Value.TemporaryFileLifetime;
        
        foreach (var path in files.EnumerateTemporary())
            if (File.GetLastWriteTimeUtc(path) < threshold.UtcDateTime)
                File.Delete(path);
        
        await db.SaveChangesAsync(ct);
        
        logger.LogInformation("Cleanup: multipart {Multipart}, orphan blobs {Orphans}", expired.Count, orphans.Count);
    }
}

/// <summary>
/// Runs cleanup periodically in a dedicated dependency-injection scope.
/// </summary>
public sealed class CleanupBackgroundService(IServiceScopeFactory scopes,
                                             IOptions<StorageOptions> options,
                                             ILogger<CleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IStorageCleanup>().RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background cleanup failed.");
            }
        } 
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
