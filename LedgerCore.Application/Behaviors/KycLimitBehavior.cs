using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace LedgerCore.Application.Behaviors;

public sealed class KycLimitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IFinancialCommand
{
    private readonly IDistributedCache _cache;
    private readonly IRequestContext _requestContext;

    public KycLimitBehavior(IDistributedCache cache, IRequestContext requestContext)
    {
        _cache = cache;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var userId = _requestContext.GetUserId();
        var key = $"KYC_Vol_24H_{userId}";

        var currentVolumeString = await _cache.GetStringAsync(key, cancellationToken);
        decimal currentVolume = 0m;

        if (!string.IsNullOrEmpty(currentVolumeString))
        {
            decimal.TryParse(currentVolumeString, out currentVolume);
        }

        var newVolume = currentVolume + request.Amount;

        if (newVolume > 10000m)
        {
            throw new InvalidOperationException("FATAL: 24-hour KYC volume limit of $10,000 exceeded.");
        }

        await _cache.SetStringAsync(key, newVolume.ToString("F2"), new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24)
        }, cancellationToken);

        return await next();
    }
}
