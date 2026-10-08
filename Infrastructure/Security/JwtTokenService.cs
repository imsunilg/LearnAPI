using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnAPI.Application.Interfaces;
using LearnAPI.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LearnAPI.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "LearnInsurance";
    public string Audience { get; set; } = "LearnInsuranceWeb";
    /// <summary>HS256 signing key, at least 32 characters. Comes from user-secrets / environment variables, never from source control in production.</summary>
    public string Key { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _o = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AuthUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_o.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("fullName", user.FullName),
            new(ClaimTypes.Email, user.Email),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key));
        var jwt = new JwtSecurityToken(
            _o.Issuer, _o.Audience, claims,
            notBefore: DateTime.UtcNow, expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    public (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, HashRefreshToken(token), DateTimeOffset.UtcNow.AddDays(_o.RefreshTokenDays));
    }

    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
