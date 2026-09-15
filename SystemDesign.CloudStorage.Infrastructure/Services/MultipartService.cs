using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Infrastructure.Storage;
using SystemDesign.CloudStorage.Persistence;

namespace SystemDesign.CloudStorage.Infrastructure.Services;

/// <summary>
/// Stores multipart parts separately and combines them through sequential reads.
/// </summary>
internal sealed class MultipartService(CloudStorageDbContext db,
                                       BlobStorage files,
                                       IObjectService objects,
                                       IOptions<StorageOptions> options,
                                       ILogger<MultipartService> logger) : IMultipartService
{
    /// <inheritdoc />
    public async Task<Guid> InitiateAsync(
        Guid owner,
        string bucket,
        string key,
        string? contentType,
        CancellationToken ct)
    {
        ObjectService.ValidateKey(key);

        var b = await db.Buckets.SingleOrDefaultAsync(x => x.OwnerId == owner && x.Name == bucket, ct) ??
                throw new AppException(404, "bucket_not_found", $"Bucket '{bucket}' not found.");

        var u = new MultipartUpload
        {
            Id = Guid.NewGuid(),
            BucketId = b.Id,
            Key = key,
            ContentType = contentType,
            Status = MultipartStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow + options.Value.MultipartLifetime
        };
        db.Add(u);
        
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Multipart upload {UploadId} started for {Bucket}/{Key}", u.Id, bucket, key);

        return u.Id;
    }

    /// <inheritdoc />
    public async Task<string> UploadPartAsync(
        Guid owner,
        Guid uploadId,
        int partNumber,
        Stream content,
        long? length,
        CancellationToken ct)
    {
        if (partNumber < 1 || partNumber > options.Value.MaxParts)
            throw new AppException(400, "invalid_part_number", "Invalid part number.");

        if (length > options.Value.MaxMultipartPartSize)
            throw new AppException(413, "part_too_large", "The part exceeds MaxMultipartPartSize.");

        var u = await Owned(owner, uploadId, ct);
        
        EnsureActive(u);
        
        var written = await files.WriteTemporaryAsync(content, options.Value.MaxMultipartPartSize, "multipart", ct);
        var previous = await db.MultipartParts.SingleOrDefaultAsync(
            x => x.MultipartUploadId == uploadId && x.PartNumber == partNumber, ct);
        
        if (previous is not null)
        {
            files.Delete(previous.RelativePath);
            db.Remove(previous);
        }

        db.Add(new MultipartPart
        {
            Id = Guid.NewGuid(),
            MultipartUploadId = uploadId,
            PartNumber = partNumber,
            RelativePath = written.RelativePath,
            Length = written.Length,
            Sha256 = written.Sha256,
            ETag = written.ETag,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Part {Part} uploaded for multipart {UploadId}, {Size} bytes", partNumber, uploadId, written.Length);

        return written.ETag;
    }

    /// <inheritdoc />
    public async Task<UploadResult> CompleteAsync(Guid owner, Guid uploadId, CancellationToken ct)
    {
        var u = await db.MultipartUploads.Include(x => x.Parts)
                    .SingleOrDefaultAsync(x => x.Id == uploadId && 
                    db.Buckets.Any(b => b.Id == x.BucketId && b.OwnerId == owner), ct) ?? throw Missing();
        
        EnsureActive(u);
        
        var parts = u.Parts.OrderBy(x => x.PartNumber).ToList();

        if (parts.Count == 0 || parts.Select((p, i) => p.PartNumber == i + 1).Any(ok => !ok))
            throw new AppException(400, "missing_parts", "Parts must have consecutive numbers starting from 1.");

        if (parts.Take(parts.Count - 1).Any(x => x.Length < options.Value.MinMultipartPartSize))
            throw new AppException(400, "part_too_small", "All parts except the last one must be at least MinMultipartPartSize.");

        u.Status = MultipartStatus.Completing;
        
        await db.SaveChangesAsync(ct);
        
        var bucket = await db.Buckets.FindAsync([u.BucketId], ct);
        
        await using var stream = new ConcatenatedReadStream([.. parts.Select(x => files.OpenRead(x.RelativePath))]);
        
        try
        {
            var result = await objects.UploadAsync(
                owner,
                bucket!.Name,
                u.Key,
                stream,
                parts.Sum(x => x.Length),
                u.ContentType,
                null,
                new Dictionary<string, string>(),
                ct);

            u.Status = MultipartStatus.Completed;
            
            foreach (var part in parts)
                files.Delete(part.RelativePath);
            
            db.RemoveRange(parts);
            
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Multipart upload {UploadId} completed, version {Version}", uploadId, result.VersionId);

            return result;
        }
        catch
        {
            u.Status = MultipartStatus.Active;
            
            await db.SaveChangesAsync(CancellationToken.None);
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AbortAsync(Guid owner, Guid uploadId, CancellationToken ct)
    {
        var u = await db.MultipartUploads.Include(x => x.Parts)
                    .SingleOrDefaultAsync(x => x.Id == uploadId &&
                            db.Buckets.Any(b => b.Id == x.BucketId && b.OwnerId == owner), ct) ?? throw Missing();
        
        if (u.Status == MultipartStatus.Completed)
            throw new AppException(409, "upload_completed", "Multipart уже завершён.");
        
        foreach (var p in u.Parts)
            files.Delete(p.RelativePath);
        
        u.Status = MultipartStatus.Aborted;
        
        db.RemoveRange(u.Parts);
        
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Multipart upload {UploadId} aborted", uploadId);
    }

    private async Task<MultipartUpload> Owned(
        Guid owner,
        Guid id,
        CancellationToken ct) => 
        await db.MultipartUploads.SingleOrDefaultAsync(x => x.Id == id &&
                          db.Buckets.Any(b => b.Id == x.BucketId && b.OwnerId == owner), ct) ?? throw Missing();

    private static void EnsureActive(MultipartUpload u)
    {
        if (u.Status != MultipartStatus.Active || u.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            throw new AppException(409, "upload_not_active", "Multipart upload is not active.");
    }

    private static AppException Missing() => new(404, "upload_not_found", "Multipart upload not found.");

    private sealed class ConcatenatedReadStream(List<Stream> streams) : Stream
    {
        private int index;
        /// <inheritdoc />
        public override bool CanRead => true;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => streams.Sum(x => x.Length);
        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            while (index < streams.Count)
            {
                var read = await streams[index].ReadAsync(buffer, ct);
                
                if (read > 0)
                    return read;
                
                await streams[index++].DisposeAsync();
            }
            return 0;
        }
        /// <inheritdoc />
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var s in streams.Skip(index))
                    s.Dispose();
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Seek(long o, SeekOrigin so) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long v) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }
}
