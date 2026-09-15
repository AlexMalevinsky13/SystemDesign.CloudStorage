namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A user registration request.
/// </summary>
public sealed record RegisterRequest(string Login, string Email, string Password);
