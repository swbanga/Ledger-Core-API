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
        // 1. Generate the deterministic Account Number
        var random = new Random();
        string generatedNumber = "0" + random.Next(100000000, 999999999).ToString();

        // 2. Parse the API strings into Strict Domain Enums
        var parsedType = Enum.Parse<AccountType>(request.AccountType, ignoreCase: true);
        var parsedTier = Enum.Parse<KycTier>(request.KycTier, ignoreCase: true);

        // 3. Instantiate via the immutable DDD constructor
        var account = new Account(
            Guid.NewGuid(),
            new AccountNumber(generatedNumber),
            parsedType,
            parsedTier
        );

        // 4. Commit to the Azure SQL Edge container
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}