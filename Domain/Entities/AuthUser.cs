namespace LearnAPI.Domain.Entities;

/// <summary>The user as returned by fn_user_get_by_username (includes the hash - never send this to the client).</summary>
public sealed record AuthUser(
    long Id,
    string Username,
    string Email,
    string FullName,
    string PasswordHash,
    bool IsActive,
    int FailedLoginCount,
    DateTimeOffset? LockedUntil,
    bool MustChangePassword,
    string[] Roles,
    string[] Permissions);
