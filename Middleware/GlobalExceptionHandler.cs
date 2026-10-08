using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace LearnAPI.Middleware;

/// <summary>
/// One place that turns exceptions into RFC 7807 ProblemDetails responses.
/// PostgreSQL functions raise business errors (P0001 → 400, P0002 → 404); constraint violations map to 409/400.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return true; // client went away - nothing to write

        var (status, title, detail, errors) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("Handled {Status} for {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status >= 500 && !env.IsDevelopment() ? "An unexpected error occurred. Please try again." : detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors is not null) problem.Extensions["errors"] = errors;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }

    private static (int Status, string Title, string Detail, IDictionary<string, string[]>? Errors) Map(Exception ex) => ex switch
    {
        ValidationException v => (400, "Validation failed", "One or more validation errors occurred.",
            v.Errors.GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
        PostgresException { SqlState: "P0001" } p => (400, "Business rule violated", p.MessageText, null),
        PostgresException { SqlState: "P0002" } p => (404, "Not found", p.MessageText, null),
        PostgresException { SqlState: "23505" } p => (409, "Duplicate value", FriendlyConstraint(p, "already exists"), null),
        PostgresException { SqlState: "23503" } p => (409, "Related record conflict", FriendlyConstraint(p, "is referenced by or references another record"), null),
        PostgresException { SqlState: "23502" } p => (400, "Missing required value", $"Required field '{p.ColumnName}' is missing.", null),
        PostgresException { SqlState: "23514" } p => (400, "Invalid value", FriendlyConstraint(p, "failed a validation rule"), null),
        PostgresException { SqlState: "22P02" or "22007" or "22008" } => (400, "Invalid value", "A value had an invalid format.", null),
        BadHttpRequestException b => (400, "Bad request", b.Message, null),
        UnauthorizedAccessException u => (403, "Forbidden", u.Message, null),
        KeyNotFoundException k => (404, "Not found", k.Message, null),
        _ => (500, "Server error", ex.Message, null),
    };

    private static string FriendlyConstraint(PostgresException p, string suffix)
    {
        var name = p.ConstraintName ?? "value";
        // ux_customers_email → "customers email"
        var pretty = string.Join(' ', name.Split('_').Skip(1));
        return string.IsNullOrWhiteSpace(pretty) ? $"The value {suffix}." : $"{char.ToUpper(pretty[0])}{pretty[1..]} {suffix}.";
    }
}
