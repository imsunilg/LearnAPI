using LearnAPI.Application.Interfaces;
using LearnAPI.Application.Services;
using LearnAPI.Infrastructure.PostgreSQL;
using LearnAPI.Infrastructure.Repositories;
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
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<ICrudService, CrudService>();
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
