using System.Text.Json;
using System.Text.RegularExpressions;
using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;

namespace LearnAPI.Infrastructure.Repositories;

public sealed partial class EntityRepository(IJsonDb db) : IEntityRepository
{
    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex EntityNameRegex();

    // The entity name is only ever supplied by controllers (constants), but we still refuse anything odd
    // because it is concatenated into a function name.
    private static string Fn(string entity, string action)
    {
        if (!EntityNameRegex().IsMatch(entity)) throw new ArgumentException("Invalid entity name", nameof(entity));
        return $"fn_{entity}_{action}";
    }

    public Task<string?> GetAllAsync(string entity, ListQuery q, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "get_all"), ct,
            q.Search, q.Page, q.PageSize, q.SortBy, q.SortDir, new Json(JsonSerializer.Serialize(q.Filters)));

    public Task<string?> GetByIdAsync(string entity, long id, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "get_by_id"), ct, id);

    public Task<string?> CreateAsync(string entity, string json, string user, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "create"), ct, new Json(json), user);

    public Task<string?> UpdateAsync(string entity, long id, string json, string user, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "update"), ct, id, new Json(json), user);

    public Task<string?> SetActiveAsync(string entity, long id, bool active, string user, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "set_active"), ct, id, active, user);

    public Task<string?> DeleteAsync(string entity, long id, string user, CancellationToken ct) =>
        db.CallAsync(Fn(entity, "delete"), ct, id, user);
}
