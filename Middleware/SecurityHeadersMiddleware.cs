using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnAPI.Middleware;

/// <summary>
/// Day 53: defensive response headers on every response. The API returns JSON and files, never HTML,
/// so the Content-Security-Policy can be the strictest possible ("nothing may load or run").
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;
        h.XContentTypeOptions = "nosniff";                        // the browser must trust our Content-Type
        h.XFrameOptions = "DENY";                                 // cannot be framed (clickjacking)
        h["Referrer-Policy"] = "no-referrer";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        h["Cross-Origin-Resource-Policy"] = "same-site";
        // Swagger UI needs inline scripts/styles, so the strict policy is not applied to /swagger.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
            h.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        h.CacheControl = "no-store";                              // API responses contain personal data: do not cache in shared caches
        return next(context);
    }
}

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    /// <summary>
    /// Fixed-window limits per client IP: a generous global limit for the whole API and a strict one for login
    /// (slows password guessing; the database also locks an account after repeated failures).
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var global = config.GetValue("RateLimiting:RequestsPerMinute", 300);
        var auth = config.GetValue("RateLimiting:AuthPerMinute", 10);

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = global, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy(AuthPolicy, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = auth, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 429, Title = "Too many requests", Detail = "You are sending requests too quickly. Please wait a moment and try again.",
                }, ct);
            };
        });
        return services;
    }
}
