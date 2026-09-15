namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A custom metadata key-value pair for an object version.
/// </summary>
public sealed class ObjectMetadata
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The object version identifier.
    /// </summary>
    public Guid ObjectVersionId { get; set; }

    /// <summary>
    /// The metadata key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The metadata value.
    /// </summary>
    public required string Value { get; set; }
}
