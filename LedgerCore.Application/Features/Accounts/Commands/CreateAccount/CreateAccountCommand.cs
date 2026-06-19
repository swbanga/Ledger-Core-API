using System;
using MediatR;

namespace LedgerCore.Application.Features.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountType,
    string KycTier) : IRequest<Guid>;
