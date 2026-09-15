namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A physical blob file descriptor.
/// </summary>
public sealed class PhysicalBlob
{
    /// <summary>
    /// The server-generated random identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The server-generated relative path.
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
    /// The creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
