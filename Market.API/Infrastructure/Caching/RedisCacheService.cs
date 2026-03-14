using System.Text.Json;
using System.Text.Json.Serialization;
using Market.API.Application.Interfaces;
using StackExchange.Redis;

namespace Market.API.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : IRedisCacheService
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);

        if (value.HasValue)
        {
            return JsonSerializer.Deserialize<T>(value!);
        }

        return default;
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        var json = JsonSerializer.Serialize(value);

        await _db.StringSetAsync(key, json, expirationTime);
    }
}
