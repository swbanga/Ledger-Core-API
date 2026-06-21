using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using StackExchange.Redis;
using System.Text.Json;

namespace LedgerCore.Application.Behaviors;

public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    Guid IdempotencyKey { get; }
}

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly IConnectionMultiplexer _redis;

    public IdempotencyBehavior(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = request.IdempotencyKey.ToString();

        // Try to atomically lock this idempotency key.
        bool acquired = await db.StringSetAsync(key, "LOCKED", TimeSpan.FromMinutes(5), When.NotExists);
        if (!acquired)
        {
            // Key already exists – could be a duplicate request.
            var existing = await db.StringGetAsync(key);
            if (existing == "LOCKED")
            {
                // Another request is currently processing this idempotency key.
                throw new InvalidOperationException("Duplicate request detected. Idempotency key already being processed.");
            }

            // A previous request has completed; return the stored response.
            return JsonSerializer.Deserialize<TResponse>(existing)!;
        }

        var response = await next();

        // Store the successful response, overwriting the lock, with a longer expiration.
        var serialized = JsonSerializer.Serialize(response);
        await db.StringSetAsync(key, serialized, TimeSpan.FromHours(24));

        return response;
    }
}
