using System.Text.Json;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Renewals: due list, quote, renew, decline. The list/quote/renew logic lives in fn_renewal_* functions;
/// permission module "renewals" (GET → view, POST → create).
/// </summary>
[ApiController]
[Route("api/renewals")]
public class RenewalsController(IJsonDb db) : ControllerBase, IPermissionScoped
{
    public string PermissionModule => "renewals";
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) { "search", "page", "pageSize", "sortBy", "sortDir" };
    private string CurrentUser => User.Identity?.Name ?? "system";
    private static ContentResult Json(string? json) => new() { Content = json ?? "null", ContentType = "application/json" };

    public sealed record DeclineRequest(string? Remarks);

    /// <summary>Renewal candidates: ?days=30&renewalStatus=Due&productTypeCode=MOTOR&search=...</summary>
    [HttpGet]
    public async Task<IActionResult> GetDue([FromQuery] ListQuery query, CancellationToken ct)
    {
        var filters = Request.Query.Where(kv => !Reserved.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        return Json(await db.CallAsync("fn_renewal_get_due", ct, query.Search, Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 200),
            query.SortBy, query.SortDir, new Json(JsonSerializer.Serialize(filters))));
    }

    /// <summary>What would the renewal cost? Includes the premium comparison and the changes (NCB, IDV, plan).</summary>
    [HttpGet("{policyId:long}/quote")]
    public async Task<IActionResult> Quote(long policyId, CancellationToken ct) => Json(await db.CallAsync("fn_renewal_quote", ct, policyId));

    [HttpPost("{policyId:long}/renew")]
    public async Task<IActionResult> Renew(long policyId, CancellationToken ct) =>
        Json(await db.CallAsync("fn_renewal_create", ct, policyId, CurrentUser));

    [HttpPost("{policyId:long}/decline")]
    public async Task<IActionResult> Decline(long policyId, DeclineRequest request, CancellationToken ct) =>
        Json(await db.CallAsync("fn_renewal_decline", ct, policyId, request.Remarks, CurrentUser));
}
