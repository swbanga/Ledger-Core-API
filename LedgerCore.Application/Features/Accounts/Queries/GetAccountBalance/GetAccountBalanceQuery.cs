using System;
using MediatR;

namespace LedgerCore.Application.Features.Accounts.Queries.GetAccountBalance;

public sealed record GetAccountBalanceQuery(Guid AccountId) : IRequest<decimal>;