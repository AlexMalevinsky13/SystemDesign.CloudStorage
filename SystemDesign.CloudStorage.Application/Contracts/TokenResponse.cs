namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// An access and refresh token pair.
/// </summary>
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);
