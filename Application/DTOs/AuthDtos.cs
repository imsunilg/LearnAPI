namespace LearnAPI.Application.DTOs;

public sealed record LoginRequest(string Username, string Password);

public sealed record UserInfo(long Id, string Username, string FullName, string Email, string[] Roles, string[] Permissions);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserInfo User);

/// <summary>Result of a login/refresh: the body for the client plus the refresh token that goes into the httpOnly cookie.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTimeOffset RefreshExpiresAt);
