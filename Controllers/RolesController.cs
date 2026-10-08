using System.Text.Json;
using System.Text.Json.Nodes;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/roles")]
public class RolesController(ICrudService service, IJsonDb db) : CrudControllerBase<RoleRequest>(service)
{
    protected override string Entity => "role";
    public override string PermissionModule => "roles";

    /// <summary>System roles (Admin, Agent...) and roles that still have users cannot be deactivated.</summary>
    public override async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var role = JsonNode.Parse(await Service.GetByIdAsync(Entity, id, ct) ?? throw new NotFoundException($"Role {id} was not found."))!;
        if (role["isSystem"]!.GetValue<bool>()) throw new BusinessRuleException("System roles cannot be deleted.");
        if (role["userCount"]!.GetValue<long>() > 0) throw new BusinessRuleException("Remove this role from its users first.");
        return await base.Delete(id, ct);
    }

    [HttpGet("{id:long}/permissions")]
    public async Task<IActionResult> GetPermissions(long id, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_role_get_permissions", ct, id));

    /// <summary>Changing permissions needs roles.edit. Users pick the change up when their token is next refreshed (≤ 30 min) or they sign in again.</summary>
    [HttpPut("{id:long}/permissions")]
    public async Task<IActionResult> SetPermissions(long id, SetPermissionsRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_role_set_permissions", ct, id, new Json(JsonSerializer.Serialize(request.Permissions)), CurrentUser));

    [HttpGet("/api/permissions/catalog")]
    [HasPermission("roles.view")]
    public async Task<IActionResult> Catalog(CancellationToken ct) => JsonResult(await db.CallAsync("fn_permission_catalog", ct));
}
