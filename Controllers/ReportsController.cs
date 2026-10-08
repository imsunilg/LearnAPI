using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IJsonDb db) : ControllerBase
{
    /// <summary>Every KPI and chart series for the dashboard in one call (fn_report_dashboard). Defaults to the last 12 months.</summary>
    [HttpGet("dashboard")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        if (from is not null && to is not null && from > to) throw new BusinessRuleException("'from' must be on or before 'to'.");
        var json = await db.CallAsync("fn_report_dashboard", ct, from, to);
        return new ContentResult { Content = json ?? "null", ContentType = "application/json" };
    }
}
