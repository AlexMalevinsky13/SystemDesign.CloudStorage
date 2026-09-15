namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A login request using either a login name or email address.
/// </summary>
public sealed record LoginRequest(string LoginOrEmail, string Password);
