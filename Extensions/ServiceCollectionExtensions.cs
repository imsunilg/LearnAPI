using LearnAPI.Application.Interfaces;
using LearnAPI.Infrastructure.PostgreSQL;
using Npgsql;

namespace LearnAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("LearnInsuranceDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LearnInsuranceDb is not configured.");

        // One NpgsqlDataSource = one connection pool for the whole app.
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<IJsonDb, PostgresJsonDb>();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy("web", p => p
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-Id", "Content-Disposition")));
        return services;
    }
}
