namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A multipart upload part stored on disk.
/// </summary>
public sealed class MultipartPart
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The multipart upload identifier.
    /// </summary>
    public Guid MultipartUploadId { get; set; }

    /// <summary>
    /// The one-based part number.
    /// </summary>
    public int PartNumber { get; set; }

    /// <summary>
    /// The relative temporary file path.
    /// </summary>
    public required string RelativePath { get; set; }

    /// <summary>
    /// The content length in bytes.
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// SHA-256.
    /// </summary>
    public required string Sha256 { get; set; }

    /// <summary>
    /// The part entity tag.
    /// </summary>
    public required string ETag { get; set; }

    /// <summary>
    /// The upload timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
