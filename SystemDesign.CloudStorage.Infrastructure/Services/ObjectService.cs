using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Infrastructure.Storage;
using SystemDesign.CloudStorage.Persistence;

namespace SystemDesign.CloudStorage.Infrastructure.Services;

/// <summary>
/// Implements versioned streaming object storage.
/// </summary>
internal sealed class ObjectService(CloudStorageDbContext db,
                                    BlobStorage files,
                                    IOptions<StorageOptions> options,
                                    ILogger<ObjectService> logger) : IObjectService
{
    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(
        Guid owner,
        string bucketName,
        string key,
        Stream content,
        long? declared,
        string? contentType,
        string? fileName,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct)
    {
        
        ValidateKey(key);

        if (declared > options.Value.MaxObjectSize)
            throw new AppException(413, "payload_too_large", "The object exceeds MaxObjectSize.");

        var bucket = await OwnedBucket(owner, bucketName, ct);
        var used = await UsedBytes(owner, ct);

        if (declared is not null && used + declared > options.Value.MaxStorageBytesPerUser)
            throw new AppException(413, "storage_quota", "Storage quota exceeded.");

        var watch = Stopwatch.StartNew();

        logger.LogInformation("Upload started for {Bucket}/{Key}", bucketName, key);

        var temp = await files.WriteTemporaryAsync(content,
            Math.Min(options.Value.MaxObjectSize, options.Value.MaxStorageBytesPerUser - used),
            "temporary",
            ct);
        
        var blobId = Guid.NewGuid();
        
        WrittenFile promoted;
        
        try
        {
            promoted = files.Promote(temp, blobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move blob for {Bucket}/{Key}", bucketName, key);
            throw;
        }

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            
            await LockObject(owner, bucketName, key, ct);
            
            var obj = await db.Objects.SingleOrDefaultAsync(x => x.BucketId == bucket.Id && x.Key == key, ct);
            if (obj is null)
            {
                obj = new StoredObject { Id = Guid.NewGuid(), BucketId = bucket.Id, Key = key };
                db.Objects.Add(obj);
            }

            var sequence = await db.ObjectVersions
                .Where(x => x.StoredObjectId == obj.Id)
                .MaxAsync(x => (long?)x.Sequence, ct) ?? 0;
            
            var version = new ObjectVersion
            {
                Id = Guid.NewGuid(),
                StoredObjectId = obj.Id,
                Sequence = sequence + 1,
                ContentLength = promoted.Length,
                ContentType = contentType ?? "application/octet-stream",
                OriginalFileName = fileName,
                Sha256 = promoted.Sha256,
                ETag = promoted.ETag,
                BlobId = blobId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            
            foreach (var pair in metadata.Take(50))
                version.Metadata.Add(new ObjectMetadata
                {
                    Id = Guid.NewGuid(),
                    ObjectVersionId = version.Id,
                    Key = pair.Key[..Math.Min(pair.Key.Length, 128)],
                    Value = pair.Value[..Math.Min(pair.Value.Length, 2048)]
                });
            
            db.Blobs.Add(new PhysicalBlob
            {
                Id = blobId,
                RelativePath = promoted.RelativePath,
                Length = promoted.Length,
                Sha256 = promoted.Sha256,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            
            db.ObjectVersions.Add(version);
            
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation("Upload completed for {Bucket}/{Key}, version {Version}, {Size} bytes in {Duration} ms",
                bucketName,
                key,
                version.Id,
                promoted.Length,
                watch.ElapsedMilliseconds);

            return new(version.Id, promoted.ETag, promoted.Length, promoted.Sha256);
        }
        catch (Exception ex)
        {
            files.Delete(promoted.RelativePath);

            logger.LogError(ex, "Compensating blob deletion after a database error for {Bucket}/{Key}", bucketName, key);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DownloadDescriptor> GetAsync(
        Guid owner,
        string bucket,
        string key,
        Guid? versionId,
        CancellationToken ct)
    {
        ValidateKey(key);
        
        var b = await OwnedBucket(owner, bucket, ct);
        
        var obj = await db.Objects.SingleOrDefaultAsync(x => x.BucketId == b.Id && x.Key == key, ct) ?? throw Missing();
        
        var q = db.ObjectVersions.Where(x => x.StoredObjectId == obj.Id);
        
        var v = versionId is null
            ? await q.OrderByDescending(x => x.Sequence).Include(x => x.Metadata).FirstOrDefaultAsync(ct)
            : await q.Include(x => x.Metadata).SingleOrDefaultAsync(x => x.Id == versionId, ct);
        
        if (v is null || v.IsDeleteMarker)
            throw Missing();

        var blob = await db.Blobs.FindAsync([v.BlobId!.Value], ct) ??
            throw new AppException(500, "blob_metadata_missing", "Blob consistency violation.");

        return new(
            v.Id,
            v.ETag!,
            v.ContentType!,
            v.ContentLength,
            v.Sha256!,
            v.Metadata.ToDictionary(x => x.Key, x => x.Value),
            _ => Task.FromResult(files.OpenRead(blob.RelativePath)));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid owner, string bucket, string key, Guid? versionId, CancellationToken ct)
    {
        ValidateKey(key);
        
        var b = await OwnedBucket(owner, bucket, ct);
        
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockObject(owner, bucket, key, ct);
        
        var obj = await db.Objects.SingleOrDefaultAsync(x => x.BucketId == b.Id && x.Key == key, ct) ?? throw Missing();
        if (versionId is null)
        {
            var seq = await db.ObjectVersions.Where(x => x.StoredObjectId == obj.Id).MaxAsync(x => x.Sequence, ct);
            
            db.ObjectVersions.Add(new ObjectVersion
            {
                Id = Guid.NewGuid(),
                StoredObjectId = obj.Id,
                Sequence = seq + 1,
                IsDeleteMarker = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            
            return;
        }
        var v =
            await db.ObjectVersions.SingleOrDefaultAsync(x => x.StoredObjectId == obj.Id && x.Id == versionId, ct) ??
            throw Missing();
        
        PhysicalBlob? blob = v.BlobId is null ? null : await db.Blobs.FindAsync([v.BlobId.Value], ct);
        
        db.Remove(v);
        
        await db.SaveChangesAsync(ct);
        
        if (blob is not null && !await db.ObjectVersions.AnyAsync(x => x.BlobId == blob.Id, ct))
        {
            db.Remove(blob);
            await db.SaveChangesAsync(ct);
        }
        
        await tx.CommitAsync(ct);
        
        if (blob is not null)
            try
            {
                files.Delete(blob.RelativePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Orphan blob {BlobId} will be removed during cleanup", blob.Id);
            }
    }

    /// <inheritdoc />
    public async Task<ObjectPage> ListAsync(
        Guid owner,
        string bucket,
        string? prefix,
        string? cursor,
        int pageSize,
        CancellationToken ct)
    {
        var b = await OwnedBucket(owner, bucket, ct);
        pageSize = Math.Clamp(pageSize, 1, 1000);
        
        var after = DecodeCursor(cursor);
        var q = db.Objects.Where(x => x.BucketId == b.Id && x.Key.CompareTo(after) > 0);
        
        if (!string.IsNullOrEmpty(prefix))
            q = q.Where(x => x.Key.StartsWith(prefix));
        
        var rows = await q.OrderBy(x => x.Key)
                       .Select(x => new { x.Key, Latest = x.Versions.OrderByDescending(v => v.Sequence).First() })
                       .Where(x => !x.Latest.IsDeleteMarker)
                       .Take(pageSize + 1)
                       .ToListAsync(ct);
        
        var hasMore = rows.Count > pageSize;
        var items = rows.Take(pageSize)
                        .Select(x => new ObjectListItem(x.Key,
                                    x.Latest.Id,
                                    x.Latest.ContentLength,
                                    x.Latest.ETag!,
                                    x.Latest.CreatedAtUtc))
                        .ToList();
        
        return new(items, hasMore ? EncodeCursor(items[^1].Key) : null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ObjectVersionDto>> ListVersionsAsync(
        Guid owner,
        string bucket,
        string key,
        CancellationToken ct)
    {
        var b = await OwnedBucket(owner, bucket, ct);
        
        var id = await db.Objects
            .Where(x => x.BucketId == b.Id && x.Key == key)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw Missing();
        
        return await db.ObjectVersions.Where(x => x.StoredObjectId == id)
            .OrderByDescending(x => x.Sequence)
            .Select(x => new ObjectVersionDto(x.Id, x.IsDeleteMarker, x.ContentLength, x.ETag, x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    private async Task<Bucket> OwnedBucket(Guid owner, string name, CancellationToken ct)
    {
        return await db.Buckets.SingleOrDefaultAsync(x => x.OwnerId == owner && x.Name == name, ct) ??
            throw new AppException(404, "bucket_not_found", $"Bucket '{name}' not found.");
    }

    private async Task<long> UsedBytes(Guid owner, CancellationToken ct) =>
        await db.ObjectVersions
            .Where(v => v.BlobId != null &&
                    db.Objects.Where(o => db.Buckets.Any(b => b.Id == o.BucketId && b.OwnerId == owner))
                        .Select(o => o.Id)
                        .Contains(v.StoredObjectId))
            .SumAsync(v => (long?)v.ContentLength, ct) ?? 0;
    
    private async Task LockObject(Guid owner, string bucket, string key, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({owner + "/" + bucket + "/" + key}, 0))", ct);

    internal static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 1024 || key[0] == '/' || key.Contains('\\') ||
            key.Split('/').Any(x => x is "." or ".." or "") || key.Any(char.IsControl))
            throw new AppException(400, "invalid_object_key", "Invalid logical object key.");
    }

    private static AppException Missing() =>
        new(404, "object_not_found", "Object or version not found.");

    private static string EncodeCursor(string key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

    private static string DecodeCursor(string? c)
    {
        if (string.IsNullOrEmpty(c))
            return "";

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(c));
        }
        catch
        {
            throw new AppException(400, "invalid_cursor", "Invalid cursor.");
        }
    }
}
