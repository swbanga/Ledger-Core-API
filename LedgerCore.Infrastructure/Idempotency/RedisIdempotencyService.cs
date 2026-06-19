using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using LedgerCore.Application.Interfaces;

namespace LedgerCore.Infrastructure.Idempotency;

public sealed class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;

    public RedisIdempotencyService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> RequestExistsAsync(Guid idempotencyKey)
    {
        var existingRequest = await _cache.GetStringAsync(idempotencyKey.ToString());
        return !string.IsNullOrEmpty(existingRequest);
    }

    public async Task CreateRequestAsync(Guid idempotencyKey, string commandName)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        await _cache.SetStringAsync(idempotencyKey.ToString(), commandName, options);
    }
}
