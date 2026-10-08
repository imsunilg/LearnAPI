using LearnAPI.Application.Interfaces;
using System.Text;
using LearnAPI.Application.Services;
using LearnAPI.Common;
using LearnAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using LearnAPI.Infrastructure.Data;
using LearnAPI.Infrastructure.PostgreSQL;
using LearnAPI.Infrastructure.Repositories;
using Npgsql;

namespace LearnAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("LearnInsuranceDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LearnInsuranceDb is not configured.");

        // One NpgsqlDataSource = one connection pool for the whole app.
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<IJsonDb, PostgresJsonDb>();
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<ICrudService, CrudService>();
        services.AddMemoryCache();
        services.AddScoped<MasterDataCache>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters (set it with user-secrets or environment variables).");

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            });
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        // Secure by default: every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
        services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy("web", p => p
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() // needed for the httpOnly refresh-token cookie
            .WithExposedHeaders("X-Correlation-Id", "Content-Disposition")));
        return services;
    }
}
