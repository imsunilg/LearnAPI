using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[ApiController]
[Route("api/premium")]
public class PremiumController(IJsonDb db) : ControllerBase
{
    private ContentResult JsonOut(string? json) => new() { Content = json ?? "null", ContentType = "application/json" };

    /// <summary>Premium breakup for a product + details. The calculation itself lives in PostgreSQL (fn_premium_calculate).</summary>
    [HttpPost("calculate")]
    [HasPermission("proposals.view")]
    public async Task<IActionResult> Calculate(PremiumRequest request, CancellationToken ct)
    {
        var details = new Json(request.Details.GetRawText());
        var json = request.Save
            ? await db.CallAsync("fn_premium_calculate_and_save", ct, request.ProductId, request.CustomerId, details, User.Identity?.Name ?? "system")
            : await db.CallAsync("fn_premium_calculate", ct, request.ProductId, details);
        return JsonOut(json);
    }

    /// <summary>Rating tables used by the UI: add-ons (MOTOR_ADDON), NCB slabs (MOTOR_NCB)...</summary>
    [HttpGet("rates/{group}")]
    [HasPermission("proposals.view")]
    public async Task<IActionResult> Rates(string group, CancellationToken ct) => JsonOut(await db.CallAsync("fn_rates_get", ct, group));

    /// <summary>Suggested IDV from the model's ex-showroom price and the vehicle's age.</summary>
    [HttpGet("motor-idv")]
    [HasPermission("proposals.view")]
    public async Task<IActionResult> MotorIdv([FromQuery] string model, [FromQuery] int manufactureYear, CancellationToken ct) =>
        JsonOut(await db.CallAsync("fn_motor_idv_suggest", ct, model, manufactureYear));
}
