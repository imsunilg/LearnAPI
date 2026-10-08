using LearnAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LearnAPI.Tests;

public class StartupChecksTests
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "LearnAPI";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public void Development_is_never_blocked() =>
        StartupChecks.Validate(Config(), new Env("Development"));

    [Fact]
    public void Production_with_missing_secrets_fails_and_lists_every_problem()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => StartupChecks.Validate(Config(), new Env("Production")));
        Assert.Contains("ConnectionStrings", ex.Message);
        Assert.Contains("Jwt:Key", ex.Message);
        Assert.Contains("Cors:AllowedOrigins", ex.Message);
    }

    [Fact]
    public void Production_rejects_placeholder_and_dev_secrets()
    {
        var cfg = Config(("ConnectionStrings:LearnInsuranceDb", "Host=db;Password=CHANGE_ME"), ("Jwt:Key", "DEV-ONLY-signing-key-change-me-0123456789-abcdef"), ("Cors:AllowedOrigins:0", "https://app.example.com"));
        var ex = Assert.Throws<InvalidOperationException>(() => StartupChecks.Validate(cfg, new Env("Production")));
        Assert.Contains("ConnectionStrings", ex.Message);
        Assert.Contains("Jwt:Key", ex.Message);
        Assert.DoesNotContain("Cors", ex.Message);
    }

    [Fact]
    public void Production_with_real_configuration_passes()
    {
        var cfg = Config(("ConnectionStrings:LearnInsuranceDb", "Host=db;Password=s3cret"), ("Jwt:Key", new string('k', 40)), ("Cors:AllowedOrigins:0", "https://app.example.com"));
        StartupChecks.Validate(cfg, new Env("Production"));
    }
}
