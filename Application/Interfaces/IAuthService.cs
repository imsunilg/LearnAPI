using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, string? ip, CancellationToken ct);
    Task LogoutAsync(string? refreshToken, CancellationToken ct);
    Task<UserInfo> GetCurrentUserAsync(long userId, CancellationToken ct);
}
