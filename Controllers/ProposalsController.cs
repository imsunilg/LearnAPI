using System.Text.Json;
using System.Text.Json.Nodes;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Proposal lifecycle: Draft → Submitted → Under Review → Approved/Rejected → Issued (Day 30).
/// Writes go through fn_proposal_* functions, which recalculate the premium and enforce the workflow.
/// </summary>
[Route("api/proposals")]
public class ProposalsController(ICrudService service, IJsonDb db) : CrudControllerBase<ProposalRequest>(service)
{
    protected override string Entity => "proposal";
    public override string PermissionModule => "proposals";

    public override async Task<IActionResult> Create(ProposalRequest request, CancellationToken ct)
    {
        var json = await db.CallAsync("fn_proposal_save", ct, null, new Json(JsonSerializer.Serialize(request, JsonDefaults.Web)), CurrentUser);
        var id = JsonNode.Parse(json!)?["id"]?.GetValue<long>();
        Response.Headers.Location = $"{Request.Path}/{id}";
        return JsonResult(json, StatusCodes.Status201Created);
    }

    public override async Task<IActionResult> Update(long id, ProposalRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_proposal_save", ct, id, new Json(JsonSerializer.Serialize(request, JsonDefaults.Web)), CurrentUser));

    /// <summary>Draft → Submitted (needs proposals.create like the rest of the write endpoints).</summary>
    [HttpPost("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_proposal_submit", ct, id, CurrentUser));

    /// <summary>Underwriting decision: review / approve / reject. Only roles with policies.issue (Underwriter, Admin).</summary>
    [HttpPost("{id:long}/decision")]
    [HasPermission("policies.issue")]
    public async Task<IActionResult> Decide(long id, DecisionRequest request, CancellationToken ct)
    {
        var to = request.Decision.ToLowerInvariant() switch { "review" => "Under Review", "approve" => "Approved", _ => "Rejected" };
        return JsonResult(await db.CallAsync("fn_proposal_change_status", ct, id, to, request.Remarks, CurrentUser));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancelRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_proposal_change_status", ct, id, "Cancelled", request.Remarks, CurrentUser));

    [HttpGet("{id:long}/timeline")]
    public async Task<IActionResult> Timeline(long id, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_status_history_get", ct, "PROPOSAL", id));

    /// <summary>Deleting a proposal means cancelling it - history must stay intact.</summary>
    public override Task<IActionResult> Delete(long id, CancellationToken ct) => Cancel(id, new CancelRequest("Deleted by user"), ct);
}
