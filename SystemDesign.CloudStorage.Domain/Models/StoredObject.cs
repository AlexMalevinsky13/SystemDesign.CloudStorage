namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A logical object that is independent of its physical file path.
/// </summary>
public sealed class StoredObject
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
    /// The logical object key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The optimistic concurrency version.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// The object versions.
    /// </summary>
    public List<ObjectVersion> Versions { get; set; } = [];
}
