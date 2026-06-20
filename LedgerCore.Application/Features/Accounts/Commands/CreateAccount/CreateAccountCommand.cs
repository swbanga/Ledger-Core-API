using System;
using MediatR;
using LedgerCore.Domain.Enums;

namespace LedgerCore.Application.Features.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountNumber,
    AccountType AccountType,
    KycTier KycTier) : IRequest<Guid>;
