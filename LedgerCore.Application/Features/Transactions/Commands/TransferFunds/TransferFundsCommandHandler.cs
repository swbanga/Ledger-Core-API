using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Data;
using MediatR;

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;
    private readonly IAccountLockService _accountLockService;

    public TransferFundsCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext,
        IAccountLockService accountLockService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
        _accountLockService = accountLockService;
    }

    public async Task<Guid> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        Guid resultTxId = Guid.Empty;
        await _context.ExecuteInTransactionAsync(async () =>
        {
            await _accountLockService.AcquireRowLockAsync(request.SourceAccountId, cancellationToken);
            var sourceAccount = await _context.FindAccountAsync(request.SourceAccountId, cancellationToken);
            var destinationAccount = await _context.FindAccountAsync(request.DestinationAccountId, cancellationToken);

            if (sourceAccount is null) throw new Exception("Source not found.");
            if (destinationAccount is null) throw new Exception("Destination not found.");

            var currentUserId = _requestContext.GetUserId();
            if (sourceAccount.OwnerUserId != Guid.Empty && sourceAccount.OwnerUserId != currentUserId)
                throw new UnauthorizedAccessException("You do not own the source account.");

            var allEntries = await _context.GetLedgerEntriesForAccountAsync(request.SourceAccountId, cancellationToken);
            var credits = allEntries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Value.Amount);
            var debits = allEntries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => Math.Abs(e.Value.Amount));
            var currentBalance = credits - debits;
            var principal = request.Amount;
            var feeAmount = 2.00m;

            if (currentBalance < (principal + feeAmount)) 
                throw new System.InvalidOperationException("FATAL: Insufficient funds.");

            sourceAccount.MarkActivity();
            var metadata = new AuditMetadata(_requestContext.GetUserId(), _requestContext.GetIpAddress(), _requestContext.GetDeviceId());
            var transaction = new LedgerTransaction(Guid.NewGuid(), $"REF-{Guid.NewGuid():N}", TransactionType.PeerToPeer, Guid.NewGuid().ToString(), metadata);

            transaction.AddEntry(new LedgerEntry(Guid.NewGuid(), transaction.Id, request.SourceAccountId, new Money(-principal, request.Currency), EntryDirection.Debit));
            transaction.AddEntry(new LedgerEntry(Guid.NewGuid(), transaction.Id, request.DestinationAccountId, new Money(principal, request.Currency), EntryDirection.Credit));
            transaction.AddEntry(new LedgerEntry(Guid.NewGuid(), transaction.Id, request.SourceAccountId, new Money(-feeAmount, request.Currency), EntryDirection.Debit));
            transaction.AddEntry(new LedgerEntry(Guid.NewGuid(), transaction.Id, Guid.Parse("33333333-3333-3333-3333-333333333333"), new Money(feeAmount, request.Currency), EntryDirection.Credit));

            transaction.Post();
            await _context.AddTransactionAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            resultTxId = transaction.Id;
        }, cancellationToken);

        return resultTxId;
    }
}
