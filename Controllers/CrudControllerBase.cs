using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Thin base controller for entities backed by generated fn_&lt;entity&gt;_* functions.
/// Derived controllers only declare the route and the entity key.
/// </summary>
[ApiController]
public abstract class CrudControllerBase(ICrudService service) : ControllerBase
{
    private static readonly HashSet<string> ReservedQueryKeys =
        new(StringComparer.OrdinalIgnoreCase) { "search", "page", "pageSize", "sortBy", "sortDir" };

    /// <summary>Entity key = function prefix, e.g. "customer" → fn_customer_get_all.</summary>
    protected abstract string Entity { get; }

    protected string CurrentUser => User.Identity?.Name ?? "system";

    protected ContentResult JsonResult(string? json, int status = 200) =>
        new() { Content = json ?? "null", ContentType = "application/json", StatusCode = status };

    protected IActionResult NotFoundProblem(long id) =>
        NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = $"{Entity} {id} was not found." });

    [HttpGet]
    public virtual async Task<IActionResult> GetAll([FromQuery] ListQuery query, CancellationToken ct)
    {
        foreach (var (key, value) in Request.Query)
        {
            if (!ReservedQueryKeys.Contains(key) && !string.IsNullOrWhiteSpace(value))
                query.Filters[key] = value.ToString();
        }
        return JsonResult(await service.GetAllAsync(Entity, query, ct));
    }

    [HttpGet("{id:long}")]
    public virtual async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var json = await service.GetByIdAsync(Entity, id, ct);
        return json is null ? NotFoundProblem(id) : JsonResult(json);
    }
}
