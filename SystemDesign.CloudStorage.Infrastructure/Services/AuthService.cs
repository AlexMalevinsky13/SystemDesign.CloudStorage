using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;


namespace SystemDesign.CloudStorage.Infrastructure.Services;

/// <summary>
/// JWT authentication with hashed refresh-token rotation.
/// </summary>
public sealed class AuthService(CloudStorageDbContext db, IPasswordHasher<User> passwords, IOptions<JwtOptions> jwt) : IAuthService
{
    /// <inheritdoc />
    public async Task<TokenResponse> RegisterAsync(RegisterRequest r, CancellationToken ct)
    {
        var login = r.Login.Trim().ToLowerInvariant();
        var email = r.Email.Trim().ToLowerInvariant();

        if (login.Length is < 3 or > 64 || !email.Contains('@') || r.Password.Length < 10)
            throw new AppException(400, "validation", "Login, email, or password failed validation.");

        if (await db.Users.AnyAsync(x => x.Login == login || x.Email == email, ct))
            throw new AppException(409, "user_exists", "Login or email is already in use.");

        var u = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            Email = email,
            PasswordHash = "",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        u.PasswordHash = passwords.HashPassword(u, r.Password);
        db.Users.Add(u);

        return await IssueAsync(u, ct);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var id = r.LoginOrEmail.Trim().ToLowerInvariant();

        var u = await db.Users.SingleOrDefaultAsync(x => x.Login == id || x.Email == id, ct) ??
            throw new AppException(401, "invalid_credentials", "Invalid credentials.");

        if (passwords.VerifyHashedPassword(u, u.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
            throw new AppException(401, "invalid_credentials", "Invalid credentials.");

        return await IssueAsync(u, ct);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> RefreshAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);

        var old = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct) ??
            throw new AppException(401, "invalid_refresh_token", "The refresh token is invalid.");

        if (old.RevokedAtUtc != null || old.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            throw new AppException(401, "invalid_refresh_token", "The refresh token has expired or has been revoked.");

        old.RevokedAtUtc = DateTimeOffset.UtcNow;

        var u = await db.Users.FindAsync([old.UserId], ct) ??
            throw new AppException(401, "invalid_refresh_token", "User not found.");

        return await IssueAsync(u, ct);
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);

        var entity = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (entity is not null)
        {
            entity.RevokedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<TokenResponse> IssueAsync(User u, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now + jwt.Value.AccessTokenLifetime;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt.Value.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, u.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString())
            ]),
            Issuer = jwt.Value.Issuer,
            Audience = jwt.Value.Audience,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var access = handler.WriteToken(handler.CreateToken(descriptor));

        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = u.Id,
            TokenHash = Hash(refresh),
            CreatedAtUtc = now,
            ExpiresAtUtc = now + jwt.Value.RefreshTokenLifetime
        });

        await db.SaveChangesAsync(ct);

        return new TokenResponse(access, refresh, expires);
    }

    private static string Hash(string value) => 
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
