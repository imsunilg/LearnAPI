using System.Text.Json;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Application.Validators;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/vehicles")]
public class VehiclesController(ICrudService service, IJsonDb db) : CrudControllerBase<VehicleRequest>(service)
{
    protected override string Entity => "vehicle";
    public override string PermissionModule => "proposals";

    /// <summary>Create-or-update by registration number (the motor form calls this when a vehicle is entered).</summary>
    [HttpPost("upsert")]
    public async Task<IActionResult> Upsert(VehicleRequest request, CancellationToken ct)
    {
        var normalised = request with { RegistrationNo = VehicleRequestValidator.Normalize(request.RegistrationNo) };
        return JsonResult(await db.CallAsync("fn_vehicle_upsert", ct, new Json(JsonSerializer.Serialize(normalised, JsonDefaults.Web)), CurrentUser));
    }

    public override Task<IActionResult> Create(VehicleRequest request, CancellationToken ct) =>
        base.Create(request with { RegistrationNo = VehicleRequestValidator.Normalize(request.RegistrationNo) }, ct);
}
