namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// Filesystem storage and quota settings.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// The absolute storage path outside the application directory.
    /// </summary>
    public required string RootPath { get; set; }

    /// <summary>
    /// The maximum object size in bytes.
    /// </summary>
    public long MaxObjectSize { get; set; } = 5L * 1024 * 1024 * 1024;

    /// <summary>
    /// The maximum multipart part size in bytes.
    /// </summary>
    public long MaxMultipartPartSize { get; set; } = 512L * 1024 * 1024;

    /// <summary>
    /// The minimum multipart part size, excluding the final part.
    /// </summary>
    public long MinMultipartPartSize { get; set; } = 5L * 1024 * 1024;

    /// <summary>
    /// The maximum number of multipart parts.
    /// </summary>
    public int MaxParts { get; set; } = 10_000;

    /// <summary>
    /// The maximum number of buckets per user.
    /// </summary>
    public int MaxBucketsPerUser { get; set; } = 100;

    /// <summary>
    /// The storage quota per user in bytes.
    /// </summary>
    public long MaxStorageBytesPerUser { get; set; } = 100L * 1024 * 1024 * 1024;

    /// <summary>
    /// The minimum temporary-file age before cleanup.
    /// </summary>
    public TimeSpan TemporaryFileLifetime { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// The background cleanup interval.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The multipart upload lifetime.
    /// </summary>
    public TimeSpan MultipartLifetime { get; set; } = TimeSpan.FromDays(1);
}
