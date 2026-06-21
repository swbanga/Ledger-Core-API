using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Behaviors;

public sealed class VelocityCheckBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRateLimitedCommand
{
    private readonly ICachingService _cache;

    public VelocityCheckBehavior(ICachingService cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string cacheKey = $"VelocityLimit_{request.RateLimitEntityId}";

        long count = await _cache.AtomicIncrementAsync(cacheKey, 1);
        await _cache.SetKeyExpiryAsync(cacheKey, TimeSpan.FromMinutes(1));

        if (count > 10)
        {
            // Undo the increment so the counter stays at its previous value.
            await _cache.AtomicIncrementAsync(cacheKey, -1);
            throw new InvalidOperationException("FATAL: Velocity limit exceeded. Maximum 10 transactions per minute allowed.");
        }

        return await next();
    }
}
