using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Application.Abstractions;

/// <summary>
/// Provides authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a user.
    /// </summary>
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token and issues a new token pair.
    /// </summary>
    Task<TokenResponse> RefreshAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken);
}
