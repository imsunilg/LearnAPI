using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;

namespace LearnAPI.Application.Services;

/// <summary>Business rules shared by all CRUD modules (paging limits today; auditing in Day 53).</summary>
public sealed class CrudService(IEntityRepository repository) : ICrudService
{
    private const int MaxPageSize = 500;

    public Task<string?> GetAllAsync(string entity, ListQuery query, CancellationToken ct)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        return repository.GetAllAsync(entity, query, ct);
    }

    public Task<string?> GetByIdAsync(string entity, long id, CancellationToken ct) => repository.GetByIdAsync(entity, id, ct);
    public Task<string?> CreateAsync(string entity, string json, string user, CancellationToken ct) => repository.CreateAsync(entity, json, user, ct);
    public Task<string?> UpdateAsync(string entity, long id, string json, string user, CancellationToken ct) => repository.UpdateAsync(entity, id, json, user, ct);
    public Task<string?> SetActiveAsync(string entity, long id, bool active, string user, CancellationToken ct) => repository.SetActiveAsync(entity, id, active, user, ct);
    public Task<string?> DeleteAsync(string entity, long id, string user, CancellationToken ct) => repository.DeleteAsync(entity, id, user, ct);
}
