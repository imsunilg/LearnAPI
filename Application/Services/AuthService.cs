using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using LearnAPI.Domain.Entities;

namespace LearnAPI.Application.Services;

public sealed class AuthService(IJsonDb db, ITokenService tokens, ILogger<AuthService> logger) : IAuthService
{
    // A valid bcrypt hash used to burn the same CPU time when the username does not exist (prevents user enumeration by timing).
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing");

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        var user = await db.CallAsync<AuthUser>("fn_user_get_by_username", ct, request.Username);

        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyHash);
            throw new UnauthorizedException("Invalid username or password.");
        }
        if (user.LockedUntil is { } until && until > DateTimeOffset.UtcNow)
            throw new UnauthorizedException($"Account is locked after too many failed attempts. Try again after {until.ToLocalTime():HH:mm}.");

        if (!user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user.IsActive) await db.CallAsync("fn_auth_login_failed", ct, user.Id);
            logger.LogWarning("Failed login for {Username} from {Ip}", request.Username, ip);
            throw new UnauthorizedException("Invalid username or password.");
        }

        await db.CallAsync("fn_auth_login_succeeded", ct, user.Id);
        return await IssueAsync(user, ip, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, string? ip, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.CallAsync<RefreshTokenRecord>("fn_refresh_token_get", ct, hash)
            ?? throw new UnauthorizedException("Session expired. Please sign in again.");

        if (stored.RevokedAt is not null)
        {
            // A revoked token being replayed = possible theft: kill every session of this user.
            await db.CallAsync("fn_refresh_token_revoke_all", ct, stored.UserId);
            logger.LogWarning("Refresh token reuse detected for user {UserId}", stored.UserId);
            throw new UnauthorizedException("Session expired. Please sign in again.");
        }
        if (stored.ExpiresAt <= DateTimeOffset.UtcNow) throw new UnauthorizedException("Session expired. Please sign in again.");

        var user = await db.CallAsync<AuthUser>("fn_user_get_auth_by_id", ct, stored.UserId);
        if (user is null || !user.IsActive) throw new UnauthorizedException("Session expired. Please sign in again.");

        await db.CallAsync("fn_refresh_token_revoke", ct, hash); // rotation: each refresh token is single-use
        return await IssueAsync(user, ip, ct);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(refreshToken))
            await db.CallAsync("fn_refresh_token_revoke", ct, tokens.HashRefreshToken(refreshToken));
    }

    public async Task<UserInfo> GetCurrentUserAsync(long userId, CancellationToken ct)
    {
        var user = await db.CallAsync<AuthUser>("fn_user_get_auth_by_id", ct, userId)
            ?? throw new UnauthorizedException();
        return ToInfo(user);
    }

    private async Task<AuthResult> IssueAsync(AuthUser user, string? ip, CancellationToken ct)
    {
        var (access, expires) = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();
        await db.CallAsync("fn_refresh_token_create", ct, user.Id, refresh.Hash, refresh.ExpiresAt.UtcDateTime, ip);
        return new AuthResult(new AuthResponse(access, expires, ToInfo(user)), refresh.Token, refresh.ExpiresAt);
    }

    private static UserInfo ToInfo(AuthUser u) => new(u.Id, u.Username, u.FullName, u.Email, u.Roles, u.Permissions);

    private sealed record RefreshTokenRecord(long UserId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);
}
