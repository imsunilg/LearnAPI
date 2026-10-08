using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>User administration. Reads come from CrudControllerBase; writes hash the password before calling the DB functions.</summary>
[Route("api/users")]
public class UsersController(ICrudService service, IJsonDb db) : CrudControllerBase(service)
{
    protected override string Entity => "user";
    public override string PermissionModule => "users";

    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var payload = new JsonObject
        {
            ["username"] = request.Username.Trim(),
            ["email"] = request.Email.Trim(),
            ["fullName"] = request.FullName.Trim(),
            ["mobile"] = request.Mobile,
            ["passwordHash"] = BCrypt.Net.BCrypt.HashPassword(request.Password, 11),
            ["roleIds"] = JsonSerializer.SerializeToNode(request.RoleIds),
        };
        var json = await db.CallAsync("fn_user_create", ct, new Json(payload.ToJsonString()), CurrentUser);
        var id = JsonNode.Parse(json!)?["id"]?.GetValue<long>();
        Response.Headers.Location = $"{Request.Path}/{id}";
        return JsonResult(json, StatusCodes.Status201Created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken ct)
    {
        var json = await db.CallAsync("fn_user_update", ct, id, new Json(JsonSerializer.Serialize(request, JsonDefaults.Web)), CurrentUser);
        return JsonResult(json);
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, StatusRequest request, CancellationToken ct)
    {
        if (id == CurrentUserId && !request.IsActive) throw new BusinessRuleException("You cannot deactivate your own account.");
        return JsonResult(await Service.SetActiveAsync(Entity, id, request.IsActive, CurrentUser, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (id == CurrentUserId) throw new BusinessRuleException("You cannot deactivate your own account.");
        return JsonResult(await Service.DeleteAsync(Entity, id, CurrentUser, ct));
    }

    /// <summary>Admin resets another user's password; all of that user's sessions are revoked.</summary>
    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, ResetPasswordRequest request, CancellationToken ct)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 11);
        return JsonResult(await db.CallAsync("fn_user_set_password", ct, id, hash, true, CurrentUser));
    }
}
