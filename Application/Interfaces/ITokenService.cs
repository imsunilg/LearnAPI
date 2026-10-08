using LearnAPI.Domain.Entities;

namespace LearnAPI.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AuthUser user);

    /// <summary>Random opaque refresh token (returned to the client) and the SHA-256 hash that is stored.</summary>
    (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string token);
}
