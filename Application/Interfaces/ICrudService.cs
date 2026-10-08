using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Interfaces;

/// <summary>Application service used by the thin CRUD controllers.</summary>
public interface ICrudService
{
    Task<string?> GetAllAsync(string entity, ListQuery query, CancellationToken ct);
    Task<string?> GetByIdAsync(string entity, long id, CancellationToken ct);
    Task<string?> CreateAsync(string entity, string json, string user, CancellationToken ct);
    Task<string?> UpdateAsync(string entity, long id, string json, string user, CancellationToken ct);
    Task<string?> SetActiveAsync(string entity, long id, bool active, string user, CancellationToken ct);
    Task<string?> DeleteAsync(string entity, long id, string user, CancellationToken ct);
}
