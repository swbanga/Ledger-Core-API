using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
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

        var transaction = new LedgerTransaction
        {
            Type = TransactionType.PeerToPeer,
            Status = TransactionStatus.Pending,
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency
        };

        var debitEntry = new LedgerEntry
        {
            AccountId = request.SourceAccountId,
            Direction = EntryDirection.Debit,
            Amount = request.Amount,
            Currency = request.Currency,
            TransactionId = transaction.Id
        };

        var creditEntry = new LedgerEntry
        {
            AccountId = request.DestinationAccountId,
            Direction = EntryDirection.Credit,
            Amount = request.Amount,
            Currency = request.Currency,
            TransactionId = transaction.Id
        };

        transaction.Entries.Add(debitEntry);
        transaction.Entries.Add(creditEntry);

        transaction.Post(_timeProvider);

        _context.LedgerTransactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
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

        var transaction = new LedgerTransaction
        {
            Type = TransactionType.PeerToPeer,
            Status = TransactionStatus.Pending,
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency
        };

        var debitEntry = new LedgerEntry
        {
            AccountId = request.SourceAccountId,
            Direction = EntryDirection.Debit,
            Amount = request.Amount,
            Currency = request.Currency,
            TransactionId = transaction.Id
        };

        var creditEntry = new LedgerEntry
        {
            AccountId = request.DestinationAccountId,
            Direction = EntryDirection.Credit,
            Amount = request.Amount,
            Currency = request.Currency,
            TransactionId = transaction.Id
        };

        transaction.Entries.Add(debitEntry);
        transaction.Entries.Add(creditEntry);

        transaction.Post(_timeProvider);

        _context.LedgerTransactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
