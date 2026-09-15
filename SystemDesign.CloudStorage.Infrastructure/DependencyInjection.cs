using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Domain.Models;
using SystemDesign.CloudStorage.Infrastructure.Services;
using SystemDesign.CloudStorage.Infrastructure.Storage;

namespace SystemDesign.CloudStorage.Infrastructure;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services and JWT authentication.
    /// </summary>
    public static IServiceCollection AddCloudStorageInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<StorageOptions>()
                .Bind(config.GetSection(StorageOptions.SectionName))
                .Validate(o => Path.IsPathFullyQualified(o.RootPath), "Storage:RootPath must be an absolute path.")
                .Validate(o => !Path.GetFullPath(o.RootPath)
                .StartsWith(Path.GetFullPath(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase), "RootPath cannot be located inside the application directory.")
                .ValidateOnStart();

        services.AddOptions<JwtOptions>()
                .Bind(config.GetSection(JwtOptions.SectionName))
                .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "JWT signing key is too short.")
                .ValidateOnStart();

        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => o.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            });

        services.AddAuthorization();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<BlobStorage>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBucketService, BucketService>();
        services.AddScoped<IObjectService, ObjectService>();
        services.AddScoped<IMultipartService, MultipartService>();
        services.AddScoped<IStorageCleanup, StorageCleanup>();
        services.AddHostedService<CleanupBackgroundService>();

        return services;
    }
}
