using LearnAPI.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace LearnAPI.Infrastructure.Data;

/// <summary>
/// In-memory cache for dropdown master data (changes rarely, read on almost every form).
/// All entries share one change token, so any admin edit evicts everything with a single call.
/// </summary>
public sealed class MasterDataCache(IMemoryCache cache, IJsonDb db)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private static CancellationTokenSource _version = new(); // shared across requests (the class itself is scoped because it uses IJsonDb)

    public async Task<string?> GetByTypeAsync(string type, long? parentId, CancellationToken ct)
    {
        var key = $"md:{type.ToUpperInvariant()}:{parentId}";
        if (cache.TryGetValue(key, out string? hit)) return hit;

        var json = await db.CallAsync("fn_masterdata_get_by_type", ct, type, parentId);
        cache.Set(key, json, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl,
            ExpirationTokens = { new CancellationChangeToken(_version.Token) },
        });
        return json;
    }

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _version, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
