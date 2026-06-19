using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Domain.Enums;

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
        // 1. Route the raw string through the correct mathematically validated factory gate
        var validatedAccountNumber = request.AccountType switch
        {
            LedgerCore.Domain.Enums.AccountType.User => LedgerCore.Domain.ValueObjects.AccountNumber.CreateUserAccount(request.AccountNumber),
            LedgerCore.Domain.Enums.AccountType.AgentFloat or LedgerCore.Domain.Enums.AccountType.Merchant => LedgerCore.Domain.ValueObjects.AccountNumber.CreateAgentOrMerchantAccount(request.AccountNumber),
            _ => LedgerCore.Domain.ValueObjects.AccountNumber.CreateSystemAccount(request.AccountNumber)
        };

        // 2. Initialize the entity using object initializer syntax, bypassing deleted constructors
        var account = new LedgerCore.Domain.Entities.Account
        {
            Id = Guid.NewGuid(), // Use request.Id if your command requires a predefined Id
            AccountNumber = validatedAccountNumber,
            AccountType = request.AccountType,
            KycTier = request.KycTier
        };

        // 4. Commit to the Azure SQL Edge container
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
