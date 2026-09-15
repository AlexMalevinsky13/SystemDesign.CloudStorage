using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Api.Controllers;

/// <summary>
/// Provides registration and token endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    /// <summary>
    /// Registers a user.
    /// </summary>
    [HttpPost("register")]
    public Task<TokenResponse> Register(RegisterRequest r, CancellationToken ct) => 
        service.RegisterAsync(r, ct);

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    [HttpPost("login")]
    public Task<TokenResponse> Login(LoginRequest r, CancellationToken ct) => 
        service.LoginAsync(r, ct);

    /// <summary>
    /// Rotates a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public Task<TokenResponse> Refresh(RefreshRequest r, CancellationToken ct) => 
        service.RefreshAsync(r.RefreshToken, ct);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    [Authorize, HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RefreshRequest r, CancellationToken ct)
    {
        await service.RevokeAsync(r.RefreshToken, ct);
        return NoContent();
    }
}
