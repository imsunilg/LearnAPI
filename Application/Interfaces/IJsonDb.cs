namespace LearnAPI.Application.Interfaces;

/// <summary>Marks a string argument as a PostgreSQL <c>jsonb</c> parameter.</summary>
public readonly record struct Json(string? Value);

/// <summary>
/// Gateway to the PostgreSQL functions. Every call is <c>SELECT fn_name(args)::text</c> and the
/// function returns JSON, so no SQL lives in application code.
/// </summary>
public interface IJsonDb
{
    /// <summary>Calls a function returning json/jsonb and returns the raw JSON text (null if the function returned NULL).</summary>
    Task<string?> CallAsync(string function, CancellationToken ct, params object?[] args);

    /// <summary>Same as <see cref="CallAsync"/> but parses the result.</summary>
    Task<T?> CallAsync<T>(string function, CancellationToken ct, params object?[] args);

    /// <summary>Verifies the database is reachable.</summary>
    Task<bool> PingAsync(CancellationToken ct);
}
