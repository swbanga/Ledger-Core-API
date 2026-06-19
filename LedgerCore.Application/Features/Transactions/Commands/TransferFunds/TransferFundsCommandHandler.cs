using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
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

        if (currentBalance < request.Amount)
        {
            throw new Exception(
                $"FATAL: Insufficient funds. Account {request.SourceAccountId} holds {currentBalance}, but attempted to transfer {request.Amount}.");
        }
        // -----------------------------------------------------

        var transactionId = Guid.NewGuid();

        // 1. Enforce the Negative Debit
        var debitEntry = new LedgerEntry(
            Guid.NewGuid(),
            transactionId,
            request.SourceAccountId,
            -request.Amount, 
            EntryDirection.Debit);

        // 2. Maintain the Positive Credit
        var creditEntry = new LedgerEntry(
            Guid.NewGuid(),
            transactionId,
            request.DestinationAccountId,
            request.Amount,
            EntryDirection.Credit);

        var entries = new[] { debitEntry, creditEntry };

        // 3. The Absolute Mathematical Lock
        if (entries.Sum(e => e.Amount) != 0)
        {
            throw new InvalidOperationException("FATAL: Transaction entries do not balance to absolute zero.");
        }

        var transaction = new LedgerTransaction(
            transactionId,
            $"REF-{Guid.NewGuid():N}",
            TransactionType.PeerToPeer,
            new CurrencyCode(request.Currency),
            _timeProvider.GetUtcNow(),
            entries,
            new AuditMetadata(Guid.Empty, "127.0.0.1", Channel.Web));

        transaction.Post(_timeProvider);

        _context.LedgerTransactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}