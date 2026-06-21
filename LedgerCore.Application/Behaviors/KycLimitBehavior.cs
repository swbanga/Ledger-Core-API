using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using MediatR;

namespace LedgerCore.Application.Behaviors;

public sealed class KycLimitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IFinancialCommand
{
    private readonly ICachingService _cache;
    private readonly IRequestContext _requestContext;

    public KycLimitBehavior(ICachingService cache, IRequestContext requestContext)
    {
        _cache = cache;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var userId = _requestContext.GetUserId();
        var key = $"KYC_Vol_24H_{userId}";

        // Convert the amount to cents with rounding to avoid decimal fractions in Redis.
        long incrementCents = (long)(request.Amount * 100);

        long newVolumeCents = await _cache.AtomicIncrementAsync(key, incrementCents);
        // Always set the expiry, even if the key already existed.
        await _cache.SetKeyExpiryAsync(key, TimeSpan.FromHours(24));

        if (newVolumeCents > 1_000_000) // $10,000.00 expressed in cents
        {
            // Rollback the increment.
            await _cache.AtomicIncrementAsync(key, -incrementCents);
            throw new InvalidOperationException("FATAL: 24-hour KYC volume limit of $10,000 exceeded.");
        }

        return await next();
    }
}
