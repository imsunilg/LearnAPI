namespace LearnAPI.Extensions;

/// <summary>
/// Day 57: fail FAST at startup when production is mis-configured, instead of failing at 2 a.m. on the first request.
/// Secrets (connection string password, JWT key) must come from environment variables or a secret store, never from committed files.
/// </summary>
public static class StartupChecks
{
    public static void Validate(IConfiguration config, IHostEnvironment env)
    {
        if (env.IsDevelopment()) return;

        var problems = new List<string>();
        var conn = config.GetConnectionString("LearnInsuranceDb") ?? "";
        if (conn.Length == 0 || conn.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            problems.Add("ConnectionStrings:LearnInsuranceDb is not set (use the ConnectionStrings__LearnInsuranceDb environment variable).");

        var key = config["Jwt:Key"] ?? "";
        if (key.Length < 32 || key.StartsWith("DEV-ONLY", StringComparison.OrdinalIgnoreCase))
            problems.Add("Jwt:Key must be a random secret of at least 32 characters (use the Jwt__Key environment variable).");

        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(o => o.Contains("localhost", StringComparison.OrdinalIgnoreCase)) && env.IsProduction())
            problems.Add("Cors:AllowedOrigins must list the real web origin(s) in Production (Cors__AllowedOrigins__0=https://...).");

        if (problems.Count > 0)
            throw new InvalidOperationException("Invalid configuration for " + env.EnvironmentName + ":" + Environment.NewLine + " - " + string.Join(Environment.NewLine + " - ", problems));
    }
}
