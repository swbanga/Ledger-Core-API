using StackExchange.Redis;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Infrastructure.Caching;

public sealed class RedisCachingService : ICachingService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCachingService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<long> AtomicIncrementAsync(string key, long delta)
    {
        var db = _redis.GetDatabase();
        return await db.StringIncrementAsync(key, delta);
    }

    public async Task SetKeyExpiryAsync(string key, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        await db.KeyExpireAsync(key, expiry);
    }

    public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        // When.NotExists ensures atomicity
        bool acquired = await db.StringSetAsync(key, value, expiry, When.NotExists);
        return acquired;
    }

    public async Task<string?> GetAsync(string key)
    {
        var db = _redis.GetDatabase();
        var result = await db.StringGetAsync(key);
        return result.HasValue ? result.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(key, value, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
