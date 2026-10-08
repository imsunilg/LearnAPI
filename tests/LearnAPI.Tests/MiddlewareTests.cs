using LearnAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearnAPI.Tests;

public class MiddlewareTests
{
    private static async Task<DefaultHttpContext> Run(RequestDelegate? next, Action<DefaultHttpContext>? arrange = null)
    {
        var ctx = new DefaultHttpContext();
        arrange?.Invoke(ctx);
        await new CorrelationIdMiddleware(next ?? (_ => Task.CompletedTask), NullLogger<CorrelationIdMiddleware>.Instance).InvokeAsync(ctx);
        return ctx;
    }

    [Fact]
    public async Task Safe_client_id_is_reused_and_echoed()
    {
        var ctx = await Run(null, c => c.Request.Headers["X-Correlation-Id"] = "abc123def456");
        Assert.Equal("abc123def456", ctx.TraceIdentifier);
        Assert.Equal("abc123def456", ctx.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Theory]
    [InlineData("short")]                              // too short
    [InlineData("has spaces and\r\nnewline-injection")] // header injection / log forging
    [InlineData("<script>alert(1)</script>")]
    public async Task Unsafe_client_id_is_replaced(string incoming)
    {
        var ctx = await Run(null, c => c.Request.Headers["X-Correlation-Id"] = incoming);
        Assert.NotEqual(incoming, ctx.TraceIdentifier);
        Assert.Matches("^[0-9a-f]{32}$", ctx.TraceIdentifier);
    }

    [Fact]
    public async Task Missing_id_is_generated()
    {
        var ctx = await Run(null);
        Assert.Matches("^[0-9a-f]{32}$", ctx.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Fact]
    public async Task Security_headers_are_added_and_swagger_is_exempt_from_the_strict_csp()
    {
        var api = new DefaultHttpContext();
        await new SecurityHeadersMiddleware(_ => Task.CompletedTask).InvokeAsync(api);
        Assert.Equal("nosniff", api.Response.Headers.XContentTypeOptions.ToString());
        Assert.Equal("DENY", api.Response.Headers.XFrameOptions.ToString());
        Assert.Contains("default-src 'none'", api.Response.Headers.ContentSecurityPolicy.ToString());
        Assert.Equal("no-store", api.Response.Headers.CacheControl.ToString());

        var swagger = new DefaultHttpContext();
        swagger.Request.Path = "/swagger/index.html";
        await new SecurityHeadersMiddleware(_ => Task.CompletedTask).InvokeAsync(swagger);
        Assert.False(swagger.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("nosniff", swagger.Response.Headers.XContentTypeOptions.ToString());
    }

    [Fact]
    public async Task Audit_writes_state_changes_but_skips_reads_and_token_refresh()
    {
        var db = new FakeJsonDb();
        var audit = new AuditMiddleware(_ => Task.CompletedTask, NullLogger<AuditMiddleware>.Instance);

        foreach (var (method, path) in new[] { ("GET", "/api/customers"), ("POST", "/api/auth/refresh"), ("POST", "/health") })
        {
            var c = new DefaultHttpContext(); c.Request.Method = method; c.Request.Path = path;
            await audit.InvokeAsync(c, db);
        }
        Assert.Empty(db.Calls);

        var put = new DefaultHttpContext(); put.Request.Method = "PUT"; put.Request.Path = "/api/customers/42";
        await audit.InvokeAsync(put, db);
        var call = Assert.Single(db.Calls);
        Assert.Equal("fn_audit_log_write", call.Function);
        Assert.Equal("customers", call.Args[4]);
        Assert.Equal(42L, call.Args[5]);
    }

    [Fact]
    public async Task Audit_failure_never_breaks_the_request()
    {
        var db = new FakeJsonDb();
        db.Handlers["fn_audit_log_write"] = _ => throw new InvalidOperationException("audit db down");
        var ran = false;
        var audit = new AuditMiddleware(_ => { ran = true; return Task.CompletedTask; }, NullLogger<AuditMiddleware>.Instance);
        var c = new DefaultHttpContext(); c.Request.Method = "POST"; c.Request.Path = "/api/claims";
        await audit.InvokeAsync(c, db);   // must not throw
        Assert.True(ran);
    }
}
