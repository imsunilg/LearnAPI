using System.Diagnostics;
using System.Text.RegularExpressions;
using LearnAPI.Application.Interfaces;

namespace LearnAPI.Middleware;

/// <summary>
/// Gives every request an id (the client's X-Correlation-Id when it is safe, otherwise a new one), echoes it in the response,
/// puts it in every log line of the request, and exposes it as TraceIdentifier so ProblemDetails carry it too.
/// A user who reports "it failed" can quote the id and we find the exact request in the logs and the audit table.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string Header = "X-Correlation-Id";

    [GeneratedRegex("^[A-Za-z0-9_-]{8,64}$")]
    private static partial Regex Safe();

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[Header].ToString();
        var id = Safe().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N"); // never echo unvalidated input (log forging / header injection)
        context.TraceIdentifier = id;
        context.Response.Headers[Header] = id;
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            await next(context);
    }
}

/// <summary>
/// Audit trail for state-changing API calls: who, what, when, outcome, duration. Request bodies are deliberately NOT stored.
/// Auditing must never break a request, so a failed audit write is logged and swallowed.
/// </summary>
public sealed partial class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    private static readonly HashSet<string> Audited = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    [GeneratedRegex(@"^/api/(?<entity>[a-z\-]+)(?:/(?<id>\d+))?(?:/(?<action>[a-z\-]+))?", RegexOptions.IgnoreCase)]
    private static partial Regex PathPattern();

    public async Task InvokeAsync(HttpContext context, IJsonDb db)
    {
        // token refresh happens silently every few minutes: auditing it would bury the real events
        if (!Audited.Contains(context.Request.Method) || !context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/api/auth/refresh"))
        {
            await next(context);
            return;
        }

        var watch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            watch.Stop();
            await WriteAsync(context, db, (int)watch.ElapsedMilliseconds);
        }
    }

    private async Task WriteAsync(HttpContext context, IJsonDb db, int ms)
    {
        try
        {
            var path = context.Request.Path.Value ?? "";
            var m = PathPattern().Match(path);
            long? entityId = m.Groups["id"].Success ? long.Parse(m.Groups["id"].Value) : null;
            var status = context.Response.StatusCode;
            await db.CallAsync("fn_audit_log_write", CancellationToken.None,
                context.TraceIdentifier, context.User.Identity?.Name, context.Request.Method, path,
                m.Success ? m.Groups["entity"].Value.ToLowerInvariant() : null, entityId,
                m.Groups["action"].Success ? m.Groups["action"].Value.ToLowerInvariant() : null,
                status, ms, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit write failed for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }
}
