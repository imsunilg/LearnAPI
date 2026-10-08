using System.Text.Json;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

public sealed record ClaimRequest(long PolicyId, string ClaimType, DateOnly IncidentDate, string? IncidentLocation, string Description, decimal ClaimedAmount);
public sealed record ClaimStatusRequest(string Status, string? Remarks, decimal? ApprovedAmount);

/// <summary>Claims: intimation and workflow. Approving/rejecting needs claims.approve; review and document requests need claims.edit.</summary>
[Route("api/claims")]
public class ClaimsController(ICrudService service, IJsonDb db) : CrudControllerBase(service)
{
    protected override string Entity => "claim";
    public override string PermissionModule => "claims";

    [HttpPost]
    public async Task<IActionResult> Create(ClaimRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_claim_create", ct, new Json(JsonSerializer.Serialize(request, JsonDefaults.Web)), CurrentUser), StatusCodes.Status201Created);

    [HttpPost("{id:long}/status")]
    [HasPermission("claims.edit")] // explicit permission replaces the verb rule (POST would otherwise mean claims.create)
    public async Task<IActionResult> ChangeStatus(long id, ClaimStatusRequest request, CancellationToken ct)
    {
        // POST normally maps to claims.create; decisions are governed by edit / approve instead.
        var needed = request.Status is "Approved" or "Rejected" ? "claims.approve" : "claims.edit";
        if (!User.HasClaim(PermissionClaim.Type, needed)) throw new ForbiddenException($"Missing permission: {needed}");
        return JsonResult(await db.CallAsync("fn_claim_change_status", ct, id, request.Status, request.Remarks, request.ApprovedAmount, CurrentUser));
    }

    [HttpGet("{id:long}/timeline")]
    public async Task<IActionResult> Timeline(long id, CancellationToken ct) => JsonResult(await db.CallAsync("fn_status_history_get", ct, "CLAIM", id));
}
