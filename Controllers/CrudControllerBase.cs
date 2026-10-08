using System.Text.Json;
using System.Text.Json.Nodes;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>
/// Thin base controller for entities backed by generated fn_&lt;entity&gt;_* functions.
/// Derived controllers only declare the route and the entity key; there is no business logic here.
/// </summary>
[ApiController]
public abstract class CrudControllerBase(ICrudService service) : ControllerBase
{
    private static readonly HashSet<string> ReservedQueryKeys =
        new(StringComparer.OrdinalIgnoreCase) { "search", "page", "pageSize", "sortBy", "sortDir" };

    protected ICrudService Service => service;

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

/// <summary>CRUD controller with typed, validated request bodies.</summary>
public abstract class CrudControllerBase<TRequest>(ICrudService service) : CrudControllerBase(service)
    where TRequest : class
{
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TRequest request, CancellationToken ct)
    {
        var json = await Service.CreateAsync(Entity, JsonSerializer.Serialize(request, JsonDefaults.Web), CurrentUser, ct);
        var id = JsonNode.Parse(json!)?["id"]?.GetValue<long>();
        Response.Headers.Location = $"{Request.Path}/{id}";
        return JsonResult(json, StatusCodes.Status201Created);
    }

    [HttpPut("{id:long}")]
    public virtual async Task<IActionResult> Update(long id, [FromBody] TRequest request, CancellationToken ct) =>
        JsonResult(await Service.UpdateAsync(Entity, id, JsonSerializer.Serialize(request, JsonDefaults.Web), CurrentUser, ct));

    [HttpPatch("{id:long}/status")]
    public virtual async Task<IActionResult> SetStatus(long id, [FromBody] StatusRequest request, CancellationToken ct) =>
        JsonResult(await Service.SetActiveAsync(Entity, id, request.IsActive, CurrentUser, ct));

    [HttpDelete("{id:long}")]
    public virtual async Task<IActionResult> Delete(long id, CancellationToken ct) =>
        JsonResult(await Service.DeleteAsync(Entity, id, CurrentUser, ct));
}
