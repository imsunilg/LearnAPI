using System.Text.Json;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

public sealed record EndorsementRequest(long PolicyId, string Type, DateOnly EffectiveDate, JsonElement Data, string? Remarks);
public sealed record EndorsementDecisionRequest(bool Approve, string? Remarks);

/// <summary>Mid-term policy changes. Reads come from CrudControllerBase; writes call fn_endorsement_*.</summary>
[Route("api/endorsements")]
public class EndorsementsController(ICrudService service, IJsonDb db) : CrudControllerBase(service)
{
    protected override string Entity => "endorsement";
    public override string PermissionModule => "endorsements";

    [HttpPost]
    public async Task<IActionResult> Create(EndorsementRequest request, CancellationToken ct)
    {
        var json = await db.CallAsync("fn_endorsement_create", ct, request.PolicyId, request.Type.ToUpperInvariant(), request.EffectiveDate,
            new Json(request.Data.GetRawText()), request.Remarks, CurrentUser);
        return JsonResult(json, StatusCodes.Status201Created);
    }

    /// <summary>Approve or reject. Underwriting authority (policies.issue) is required.</summary>
    [HttpPost("{id:long}/decision")]
    [HasPermission("policies.issue")]
    public async Task<IActionResult> Decide(long id, EndorsementDecisionRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_endorsement_decide", ct, id, request.Approve, request.Remarks, CurrentUser));
}
