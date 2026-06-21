using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using StackExchange.Redis;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Behaviors;

public sealed class VelocityCheckBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRateLimitedCommand
{
    private readonly IConnectionMultiplexer _redis;

    public VelocityCheckBehavior(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        string cacheKey = $"VelocityLimit_{request.RateLimitEntityId}";

        long count = await db.StringIncrementAsync(cacheKey);
        await db.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(1));

        if (count > 10)
        {
            // Undo the increment so the counter stays at its previous value.
            await db.StringIncrementAsync(cacheKey, -1);
            throw new InvalidOperationException("FATAL: Velocity limit exceeded. Maximum 10 transactions per minute allowed.");
        }

        return await next();
    }
}
