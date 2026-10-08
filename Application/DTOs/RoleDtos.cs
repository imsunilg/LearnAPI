namespace LearnAPI.Application.DTOs;

public sealed record RoleRequest(string Name, string? Description);

public sealed record SetPermissionsRequest(string[] Permissions);
