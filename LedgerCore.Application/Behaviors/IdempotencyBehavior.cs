using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LedgerCore.Application.Behaviors;

public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    Guid IdempotencyKey { get; }
}

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly IDistributedCache _cache;

    public IdempotencyBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKey = request.IdempotencyKey.ToString();
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();
        var serialized = JsonSerializer.Serialize(response);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        return response;
    }
}
