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

    public sealed record IssueRequest(long ProposalId);

    [HttpPost("issue")]
    [HasPermission("policies.issue")]
    public async Task<IActionResult> Issue(IssueRequest request, CancellationToken ct)
    {
        var json = await db.CallAsync("fn_policy_create", ct, request.ProposalId, CurrentUser);
        return JsonResult(json, StatusCodes.Status201Created);
    }
}
