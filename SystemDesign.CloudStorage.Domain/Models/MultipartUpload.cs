namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A multipart upload session.
/// </summary>
public sealed class MultipartUpload
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Bucket.
    /// </summary>
    public Guid BucketId { get; set; }

    /// <summary>
    /// The destination object key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    public MultipartStatus Status { get; set; }

    /// <summary>
    /// The creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// The expiration timestamp for an incomplete session.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// The uploaded parts.
    /// </summary>
    public List<MultipartPart> Parts { get; set; } = [];
}
