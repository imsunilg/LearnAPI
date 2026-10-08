using System.Security.Claims;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private const string RefreshCookie = "li_rt";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ClientIp, ct);
        SetRefreshCookie(result);
        return result.Response;
    }

    /// <summary>Exchanges the httpOnly refresh cookie for a new access token (and rotates the cookie).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var token) || string.IsNullOrEmpty(token))
            throw new UnauthorizedException("No active session.");
        try
        {
            var result = await auth.RefreshAsync(token, ClientIp, ct);
            SetRefreshCookie(result);
            return result.Response;
        }
        catch (UnauthorizedException)
        {
            Response.Cookies.Delete(RefreshCookie, CookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookie, out var token);
        await auth.LogoutAsync(token, ct);
        Response.Cookies.Delete(RefreshCookie, CookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> Me(CancellationToken ct) =>
        await auth.GetCurrentUserAsync(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private CookieOptions CookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,                 // JavaScript (and therefore XSS) can never read the refresh token
        Secure = Request.IsHttps,        // true in production behind HTTPS
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",              // sent only to auth endpoints
        Expires = expires,
    };

    private void SetRefreshCookie(AuthResult r) => Response.Cookies.Append(RefreshCookie, r.RefreshToken, CookieOptions(r.RefreshExpiresAt));
}
