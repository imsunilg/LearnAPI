using System.Text.Json;

namespace LearnAPI;

public static class JsonDefaults
{
    /// <summary>camelCase, case-insensitive reads, nulls are written (so PUT can clear a field).</summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
