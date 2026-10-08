using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(IJsonDb db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbOk = await db.PingAsync(ct);
        return Ok(new { status = dbOk ? "Healthy" : "Degraded", database = dbOk ? "up" : "down", serverTimeUtc = DateTime.UtcNow });
    }
}
