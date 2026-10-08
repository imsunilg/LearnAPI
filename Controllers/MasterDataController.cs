using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Master data admin CRUD (permission module "masterdata") plus cached dropdown reads.
/// Every write evicts the cache so changes show up immediately.
/// </summary>
[Route("api/master-data")]
public class MasterDataController(ICrudService service, IJsonDb db, MasterDataCache cache) : CrudControllerBase<MasterDataRequest>(service)
{
    protected override string Entity => "master_data";
    public override string PermissionModule => "masterdata";

    /// <summary>Dropdown data: GET /api/master-data/by-type/CITY?parentId=3. Any signed-in user may read it (cached 15 min).</summary>
    [HttpGet("by-type/{type}")]
    public async Task<IActionResult> ByType(string type, [FromQuery] long? parentId, CancellationToken ct) =>
        JsonResult(await cache.GetByTypeAsync(type, parentId, ct));

    [HttpGet("types")]
    public async Task<IActionResult> Types(CancellationToken ct) => JsonResult(await db.CallAsync("fn_masterdata_get_types", ct));

    public override async Task<IActionResult> Create(MasterDataRequest request, CancellationToken ct)
    {
        var result = await base.Create(request, ct);
        cache.Invalidate();
        return result;
    }

    public override async Task<IActionResult> Update(long id, MasterDataRequest request, CancellationToken ct)
    {
        var result = await base.Update(id, request, ct);
        cache.Invalidate();
        return result;
    }

    public override async Task<IActionResult> SetStatus(long id, StatusRequest request, CancellationToken ct)
    {
        var result = await base.SetStatus(id, request, ct);
        cache.Invalidate();
        return result;
    }

    public override async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await base.Delete(id, ct);
        cache.Invalidate();
        return result;
    }
}
