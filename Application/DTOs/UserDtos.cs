namespace LearnAPI.Application.DTOs;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string FullName,
    string? Mobile,
    string Password,
    long[] RoleIds);

public sealed record UpdateUserRequest(
    string Email,
    string FullName,
    string? Mobile,
    long[] RoleIds);

public sealed record ResetPasswordRequest(string NewPassword);
