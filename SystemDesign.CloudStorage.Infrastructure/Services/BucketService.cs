using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Persistence;

namespace SystemDesign.CloudStorage.Infrastructure.Services;

/// <summary>
/// Manages buckets and enforces ownership.
/// </summary>
public sealed class BucketService(CloudStorageDbContext db, IOptions<StorageOptions> options) : IBucketService
{
    private static readonly Regex BucketNameRegex = 
        new("^[a-z0-9](?:[a-z0-9.-]{1,61}[a-z0-9])?$", RegexOptions.Compiled);

    private static bool IsValidBucketName(string name) =>
        BucketNameRegex.IsMatch(name) && !name.Contains("..");

    /// <inheritdoc />
    public async Task<BucketDto> CreateAsync(Guid owner, string name, CancellationToken ct)
    {
        name = name.Trim().ToLowerInvariant();

        if (!IsValidBucketName(name))
            throw new AppException(400, "invalid_bucket_name", "Invalid bucket name.");

        if (await db.Buckets.CountAsync(x => x.OwnerId == owner, ct) >= options.Value.MaxBucketsPerUser)
            throw new AppException(409, "bucket_quota", "The bucket limit has been reached.");

        if (await db.Buckets.AnyAsync(x => x.OwnerId == owner && x.Name == name, ct))
            throw new AppException(409, "bucket_exists", "The bucket already exists.");

        var bucket = new Bucket
        {
            Id = Guid.NewGuid(),
            OwnerId = owner,
            Name = name,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Add(bucket);

        await db.SaveChangesAsync(ct);

        return new BucketDto(bucket.Name, bucket.CreatedAtUtc);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BucketDto>> ListAsync(Guid owner,
        CancellationToken ct) => await db.Buckets.Where(x => x.OwnerId == owner)
                                     .OrderBy(x => x.Name)
                                     .Select(x => new BucketDto(x.Name, x.CreatedAtUtc))
                                     .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<BucketDto> GetAsync(Guid owner,string name, CancellationToken ct) => 
        await db.Buckets.Where(x => x.OwnerId == owner && x.Name == name)
                                     .Select(x => new BucketDto(x.Name, x.CreatedAtUtc))
                                     .SingleOrDefaultAsync(ct) ??
        throw new AppException(404, "bucket_not_found", $"Bucket {name} not found.");

    /// <inheritdoc />
    public async Task DeleteAsync(Guid owner, string name, CancellationToken ct)
    {
        var b = await db.Buckets.SingleOrDefaultAsync(x => x.OwnerId == owner && x.Name == name, ct) ??
            throw new AppException(404, "bucket_not_found", $"Bucket {name} not found.");

        if (await db.Objects.AnyAsync(x => x.BucketId == b.Id, ct))
            throw new AppException(409, "bucket_not_empty", $"A non-empty bucket {name} cannot be deleted.");

        db.Remove(b);
        
        await db.SaveChangesAsync(ct);
    }
}
