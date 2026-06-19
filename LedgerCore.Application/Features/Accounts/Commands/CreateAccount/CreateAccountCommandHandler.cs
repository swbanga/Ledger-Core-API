using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Application.Features.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CreateAccountCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Generate a pseudo-random 10-digit account number starting with 0
        var random = new Random();
        string generatedNumber = "0" + random.Next(100000000, 999999999).ToString();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = new AccountNumber(generatedNumber),
            // In a production system, these strings would be strictly validated Enums.
            AccountType = request.AccountType,
            KycTier = request.KycTier
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
