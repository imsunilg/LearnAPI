namespace LearnAPI.Application.DTOs;

public sealed record ProductTypeRequest(string Code, string Name, string? Description, string? Icon, int? SortOrder);

public sealed record ProductRequest(
    string Name,
    long ProductTypeId,
    string? Description,
    decimal BaseRate,
    decimal? MinSumInsured,
    decimal MaxSumInsured,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);
