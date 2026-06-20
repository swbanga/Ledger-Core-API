using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Projections;
using LedgerCore.Application.Exceptions;

namespace LedgerCore.Application.Features.Queries.GetAccountBalance;

public sealed class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalance>
{
    private readonly IDistributedCache _cache;
    private readonly IApplicationDbContext _context;

    public GetAccountBalanceQueryHandler(IDistributedCache cache, IApplicationDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public async Task<AccountBalance> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var key = $"AccountBalance_{request.AccountId}";
        var cached = await _cache.GetStringAsync(key, cancellationToken);

        if (cached != null)
        {
            return JsonSerializer.Deserialize<AccountBalance>(cached)!;
        }

        var balance = await _context.AccountBalances
            .FirstOrDefaultAsync(b => b.AccountId == request.AccountId, cancellationToken);

        if (balance == null)
        {
            throw new NotFoundException($"AccountBalance not found for account ID {request.AccountId}.");
        }

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(balance),
            cacheOptions,
            cancellationToken);

        return balance;
    }
}
