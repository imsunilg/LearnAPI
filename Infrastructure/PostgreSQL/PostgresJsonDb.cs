using System.Text.Json;
using LearnAPI.Application.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace LearnAPI.Infrastructure.PostgreSQL;

public sealed class PostgresJsonDb(NpgsqlDataSource dataSource, ILogger<PostgresJsonDb> logger) : IJsonDb
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> CallAsync(string function, CancellationToken ct, params object?[] args)
    {
        // Function names come from code (never from user input), arguments are always parameters.
        var placeholders = string.Join(", ", args.Select((_, i) => $"${i + 1}"));
        await using var cmd = dataSource.CreateCommand($"SELECT learninsurancedb.{function}({placeholders})::text");
        foreach (var arg in args) cmd.Parameters.Add(ToParameter(arg));

        logger.LogDebug("DB call {Function}", function);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? null : (string)result;
    }

    public async Task<T?> CallAsync<T>(string function, CancellationToken ct, params object?[] args)
    {
        var json = await CallAsync(function, ct, args);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("SELECT 1");
        return await cmd.ExecuteScalarAsync(ct) is 1;
    }

    private static NpgsqlParameter ToParameter(object? value) => value switch
    {
        null => new NpgsqlParameter { Value = DBNull.Value, NpgsqlDbType = NpgsqlDbType.Unknown },
        Json j => new NpgsqlParameter { Value = (object?)j.Value ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Jsonb },
        string s => new NpgsqlParameter { Value = s, NpgsqlDbType = NpgsqlDbType.Text },
        DateOnly d => new NpgsqlParameter { Value = d, NpgsqlDbType = NpgsqlDbType.Date },
        DateTime dt => new NpgsqlParameter { Value = DateTime.SpecifyKind(dt, DateTimeKind.Utc), NpgsqlDbType = NpgsqlDbType.TimestampTz },
        _ => new NpgsqlParameter { Value = value },
    };
}
