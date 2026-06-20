using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Behaviors;

public sealed class VelocityCheckBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRateLimitedCommand
{
    private readonly IDistributedCache _cache;

    public VelocityCheckBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string cacheKey = $"VelocityLimit_{request.RateLimitEntityId}";

        var existing = await _cache.GetAsync(cacheKey, cancellationToken);
        string? currentValue = existing != null ? Encoding.UTF8.GetString(existing) : null;

        if (string.IsNullOrEmpty(currentValue))
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes("1"), options, cancellationToken);
        }
        else
        {
            int count = int.Parse(currentValue);
            if (count >= 10)
            {
                throw new InvalidOperationException("FATAL: Velocity limit exceeded. Maximum 10 transactions per minute allowed.");
            }

            count++;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(count.ToString()), options, cancellationToken);
        }

        return await next();
    }
}
