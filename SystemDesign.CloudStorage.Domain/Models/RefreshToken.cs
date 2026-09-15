namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A hashed user refresh token.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The owner identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The SHA-256 hash of the original token.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// The expiration timestamp in UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// The revocation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// The creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
