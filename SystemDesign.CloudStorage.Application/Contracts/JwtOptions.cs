namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// JWT authentication settings.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The token issuer.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// The token audience.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// The signing secret, at least 32 bytes long.
    /// </summary>
    public required string SigningKey { get; set; }

    /// <summary>
    /// The access token lifetime.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The refresh token lifetime.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}
