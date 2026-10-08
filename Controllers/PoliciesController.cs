using System.Text.Json;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Policies are read-only through the generic endpoints (list/get). The only way to create one is issuing an approved
/// proposal, which requires the policies.issue permission (Underwriter, Admin).
/// </summary>
[Route("api/policies")]
public class PoliciesController(ICrudService service, IJsonDb db) : CrudControllerBase(service)
{
    protected override string Entity => "policy";
    public override string PermissionModule => "policies";

    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase) { "search", "page", "pageSize", "sortBy", "sortDir" };

    /// <summary>
    /// Advanced search: every query-string key other than paging/sorting is passed to fn_policy_search as a filter
    /// (effectiveStatus, productTypeCode, agentId, issuedFrom/To, premiumMin/Max, expiringInDays ...).
    /// </summary>
    public override async Task<IActionResult> GetAll([FromQuery] ListQuery query, CancellationToken ct)
    {
        var filters = Request.Query.Where(kv => !ReservedKeys.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var json = await db.CallAsync("fn_policy_search", ct, query.Search, Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 200),
            query.SortBy, query.SortDir, new Json(JsonSerializer.Serialize(filters)));
        return JsonResult(json);
    }

    public sealed record IssueRequest(long ProposalId);

    [HttpPost("issue")]
    [HasPermission("policies.issue")]
    public async Task<IActionResult> Issue(IssueRequest request, CancellationToken ct)
    {
        var json = await db.CallAsync("fn_policy_create", ct, request.ProposalId, CurrentUser);
        return JsonResult(json, StatusCodes.Status201Created);
    }
}
