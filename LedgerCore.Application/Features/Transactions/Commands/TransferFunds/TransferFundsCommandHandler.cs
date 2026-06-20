using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TransferFundsCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var sourceAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken);

        var destinationAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken);

        if (sourceAccount is null)
            throw new Exception($"Source account {request.SourceAccountId} not found.");

        if (destinationAccount is null)
            throw new Exception($"Destination account {request.DestinationAccountId} not found.");

        // --- NEW ENFORCEMENT: THE OVERDRAFT DEFENSE SHIELD ---
        var currentBalance = await _context.LedgerEntries
            .Where(e => e.AccountId == request.SourceAccountId)
            .SumAsync(e => e.Amount, cancellationToken);

        var principal = request.Amount;
        var systemFee = 1.50m;
        var zimraTax = 0.50m;
        var totalDebit = principal + systemFee + zimraTax;

        if (currentBalance < totalDebit)
        {
            throw new LedgerCore.Domain.Exceptions.InsufficientFundsException(
                $"FATAL: Insufficient funds. Account {request.SourceAccountId} holds {currentBalance}, but attempted to transfer {request.Amount} (total including fees/taxes: {totalDebit}).");
        }
        // -----------------------------------------------------

        var transactionId = Guid.NewGuid();

        // Execute the 4-Leg Multi-Routing Matrix
        var entries = new List<LedgerCore.Domain.Entities.LedgerEntry>
        {
            new() { Id = Guid.NewGuid(), AccountId = request.SourceAccountId, Amount = -totalDebit, Direction = LedgerCore.Domain.Enums.EntryDirection.Debit },
            new() { Id = Guid.NewGuid(), AccountId = request.DestinationAccountId, Amount = principal, Direction = LedgerCore.Domain.Enums.EntryDirection.Credit },
            new() { Id = Guid.NewGuid(), AccountId = SystemAccountIds.SystemRevenue, Amount = systemFee, Direction = LedgerCore.Domain.Enums.EntryDirection.Credit },
            new() { Id = Guid.NewGuid(), AccountId = SystemAccountIds.TaxLiabilityZimra, Amount = zimraTax, Direction = LedgerCore.Domain.Enums.EntryDirection.Credit }
        };

        var transaction = new LedgerTransaction(
            transactionId,
            $"REF-{Guid.NewGuid():N}",
            TransactionType.PeerToPeer,
            new CurrencyCode(request.Currency),
            _timeProvider.GetUtcNow(),
            new List<LedgerCore.Domain.Entities.LedgerEntry>(),
            new AuditMetadata(Guid.Empty, "127.0.0.1", Channel.Web));

        // Add all 4 legs to the aggregate root
        foreach (var entry in entries)
        {
            transaction.Entries.Add(entry);
        }

        // The Absolute Mathematical Invariant
        if (transaction.Entries.Sum(e => e.Amount) != 0)
        {
            throw new InvalidOperationException("FATAL: Double-entry invariant violated. Multi-leg transaction does not balance to absolute zero.");
        }

        transaction.Post(_timeProvider);

        _context.LedgerTransactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
