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

        // First, check if we already have a stored result.
        var existing = await _cache.GetAsync(key);
        if (existing != null)
        {
            // If another request is still processing this key, reject the duplicate.
            if (existing == "LOCKED")
            {
                throw new InvalidOperationException("Duplicate request detected. Idempotency key already being processed.");
            }

            // A previous request has completed; return the stored response.
            return JsonSerializer.Deserialize<TResponse>(existing);
        }

        // No cached entry yet — try to acquire the lock.
        bool acquired = await _cache.AcquireLockAsync(key, "LOCKED", TimeSpan.FromMinutes(5));
        if (!acquired)
        {
            // Another request grabbed the lock in a race between our null‑check and now.
            throw new InvalidOperationException("Duplicate request detected. Idempotency key already being processed.");
        }

        try
        {
            var response = await next();

            // Store the successful response, overwriting the lock, with a longer expiration.
            var serialized = JsonSerializer.Serialize(response);
            await _cache.SetAsync(key, serialized, TimeSpan.FromHours(24));

            return response;
        }
        catch
        {
            // On failure we do NOT store any result; the key may be used again in a future attempt.
            throw;
        }
    }
}
