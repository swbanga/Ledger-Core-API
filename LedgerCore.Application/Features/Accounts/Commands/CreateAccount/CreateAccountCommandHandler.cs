using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Features.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Route the raw string through the mathematically validated factory gate
        var validatedAccountNumber = request.AccountType switch
        {
            AccountType.User => AccountNumber.CreateUserAccount(request.AccountNumber),
            AccountType.AgentFloat or AccountType.Merchant => AccountNumber.CreateAgentOrMerchantAccount(request.AccountNumber),
            _ => AccountNumber.CreateSystemAccount(request.AccountNumber)
        };

        // 2. Initialize the entity using explicit property syntax
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = validatedAccountNumber,
            AccountType = request.AccountType,
            KycTier = request.KycTier
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
