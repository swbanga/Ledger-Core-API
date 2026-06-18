using System;
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

        var transactionId = Guid.NewGuid();

        var debitEntry = new LedgerEntry(
            Guid.NewGuid(),
            transactionId,
            request.SourceAccountId,
            request.Amount,
            EntryDirection.Debit);

        var creditEntry = new LedgerEntry(
            Guid.NewGuid(),
            transactionId,
            request.DestinationAccountId,
            request.Amount,
            EntryDirection.Credit);

        var entries = new[] { debitEntry, creditEntry };

        var transaction = new LedgerTransaction(
            Guid.NewGuid(),
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
