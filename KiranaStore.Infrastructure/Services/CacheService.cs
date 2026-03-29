using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using KiranaStore.Application.Interfaces;

namespace KiranaStore.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    public CacheService(IDistributedCache cache) { _cache = cache; }

    public async Task<T?> GetAsync<T>(string key)
    {
        try { var d = await _cache.GetStringAsync(key); return d == null ? default : JsonSerializer.Deserialize<T>(d); }
        catch { return default; }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try { await _cache.SetStringAsync(key, JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30) }); }
        catch { }
    }

    public async Task RemoveAsync(string key) { try { await _cache.RemoveAsync(key); } catch { } }
}
