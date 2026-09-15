namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A user-owned object namespace.
/// </summary>
public sealed class Bucket
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The owner identifier.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
