namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A refresh token request.
/// </summary>
public sealed record RefreshRequest(string RefreshToken);
