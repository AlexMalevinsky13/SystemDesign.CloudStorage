namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// A storage user.
/// </summary>
public sealed class User
{
    /// <summary>
    /// The identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique login name.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// The unique email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The password hash.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// The registration timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
