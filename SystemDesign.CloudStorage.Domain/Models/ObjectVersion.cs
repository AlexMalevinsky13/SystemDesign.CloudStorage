namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// An immutable logical object version or delete marker.
/// </summary>
public sealed class ObjectVersion
{
    /// <summary>
    /// The version identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The logical object identifier.
    /// </summary>
    public Guid StoredObjectId { get; set; }

    /// <summary>
    /// The sequence number within the object.
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// Indicates whether this version is a delete marker.
    /// </summary>
    public bool IsDeleteMarker { get; set; }

    /// <summary>
    /// The original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// The MIME content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The content length in bytes.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// SHA-256.
    /// </summary>
    public string? Sha256 { get; set; }

    /// <summary>
    /// HTTP ETag.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// The creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// The physical blob identifier.
    /// </summary>
    public Guid? BlobId { get; set; }

    /// <summary>
    /// The custom object metadata.
    /// </summary>
    public List<ObjectMetadata> Metadata { get; set; } = [];
}
