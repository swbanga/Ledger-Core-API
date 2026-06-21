using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using System.Text.Json;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Behaviors;

public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    Guid IdempotencyKey { get; }
}

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly ICachingService _cache;

    public IdempotencyBehavior(ICachingService cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var key = request.IdempotencyKey.ToString();

        // Try to atomically lock this idempotency key.
        bool acquired = await _cache.AcquireLockAsync(key, "LOCKED", TimeSpan.FromMinutes(5));
        if (!acquired)
        {
            // Key already exists – could be a duplicate request.
            var existing = await _cache.GetAsync(key);
            if (existing == "LOCKED")
            {
                // Another request is currently processing this idempotency key.
                throw new InvalidOperationException("Duplicate request detected. Idempotency key already being processed.");
            }

            // A previous request has completed; return the stored response.
            return JsonSerializer.Deserialize<TResponse>(existing!)!;
        }

        var response = await next();

        // Store the successful response, overwriting the lock, with a longer expiration.
        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetAsync(key, serialized, TimeSpan.FromHours(24));

        return response;
    }
}
