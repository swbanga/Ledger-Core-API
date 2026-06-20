using MediatR;
using LedgerCore.Domain.Projections;

namespace LedgerCore.Application.Features.Queries.GetAccountBalance;

public sealed class GetAccountBalanceQuery : IRequest<AccountBalance>
{
    public Guid AccountId { get; }

    public GetAccountBalanceQuery(Guid accountId)
    {
        AccountId = accountId;
    }
}
