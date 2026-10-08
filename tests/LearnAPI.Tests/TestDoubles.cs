using System.Text;
using LearnAPI.Application.Interfaces;
using LearnAPI.Infrastructure.Payments;
using LearnAPI.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LearnAPI.Tests;

/// <summary>Records every function call and answers from a script, so services can be tested without PostgreSQL.</summary>
public sealed class FakeJsonDb : IJsonDb
{
    public List<(string Function, object?[] Args)> Calls { get; } = [];
    public Dictionary<string, Func<object?[], string?>> Handlers { get; } = new();

    public Task<string?> CallAsync(string function, CancellationToken ct, params object?[] args)
    {
        Calls.Add((function, args));
        return Task.FromResult(Handlers.TryGetValue(function, out var h) ? h(args) : "{}");
    }

    public async Task<T?> CallAsync<T>(string function, CancellationToken ct, params object?[] args)
    {
        var json = await CallAsync(function, ct, args);
        return json is null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }

    public Task<bool> PingAsync(CancellationToken ct) => Task.FromResult(true);
}

public sealed class FakeStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public async Task SaveAsync(string key, Stream content, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        Files[key] = ms.ToArray();
    }

    public Stream? Open(string key) => Files.TryGetValue(key, out var b) ? new MemoryStream(b) : null;
    public void Delete(string key) => Files.Remove(key);
}

public sealed class CountingGateway(bool success = true) : IPaymentGateway
{
    public int Charges { get; private set; }

    public Task<GatewayResult> ChargeAsync(GatewayRequest request, CancellationToken ct)
    {
        Charges++;
        return Task.FromResult(new GatewayResult(success, "TXN1", success ? "ok" : "declined", new System.Text.Json.Nodes.JsonObject { ["message"] = "x" }));
    }
}

public static class Make
{
    public static IFormFile File(string name, byte[] content)
    {
        var ms = new MemoryStream(content);
        return new FormFile(ms, 0, content.Length, "file", name);
    }

    public static IFormFile File(string name, string text) => File(name, Encoding.ASCII.GetBytes(text));
    public static IOptions<StorageOptions> Storage(long max = 1024 * 1024) => Options.Create(new StorageOptions { MaxFileBytes = max });
}
