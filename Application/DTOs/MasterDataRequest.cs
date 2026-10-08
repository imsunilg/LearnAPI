using System.Text.Json;

namespace LearnAPI.Application.DTOs;

public sealed record MasterDataRequest(string Type, string Code, string Name, long? ParentId, int? SortOrder, JsonElement? Meta);
