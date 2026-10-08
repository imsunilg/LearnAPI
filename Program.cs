using FluentValidation;
using LearnAPI.Common;
using LearnAPI.Extensions;
using LearnAPI.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
StartupChecks.Validate(builder.Configuration, builder.Environment);

// Behind a reverse proxy (nginx, a cloud load balancer) the TCP peer is the proxy. Opt in with Proxy__Enabled=true so that
// rate limiting, audit logs and the Secure cookie flag see the real client IP and the original https scheme.
var behindProxy = builder.Configuration.GetValue<bool>("Proxy:Enabled");
if (behindProxy)
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear(); // the proxy runs in a private network we control; do not enable this when the API is directly on the internet
        o.KnownProxies.Clear();
    });

builder.Services.AddControllers(o =>
{
    // Filters.Add<T>() wraps the type in a factory whose Order is 0, so the order must be given here, BEFORE [ApiController]'s model-state filter (-2000).
    o.Filters.Add(typeof(PermissionFilter), -3000); // authorization first...
    o.Filters.Add<ValidationFilter>(); // ...then validation
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LearnInsurance API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /api/auth/login",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (behindProxy) app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>(); // first: every log line and error body carries the id
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHsts(); // HTTPS-only in production (Strict-Transport-Security)
app.UseRateLimiter();
app.UseCors("web"); // before the exception handler so error responses still carry CORS headers
app.UseMiddleware<AuditMiddleware>(); // outside the exception handler, so it sees the final status code
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
