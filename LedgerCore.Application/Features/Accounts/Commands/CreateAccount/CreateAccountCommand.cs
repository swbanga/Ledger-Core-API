using System;
using MediatR;
using LedgerCore.Domain.Enums;
using LedgerCore.Application.Behaviors;

namespace LedgerCore.Application.Features.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountNumber,
    AccountType AccountType,
    KycTier KycTier,
    Guid IdempotencyKey) : IRequest<Guid>, IIdempotentCommand<Guid>;
