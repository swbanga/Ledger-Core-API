using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        // Use the underlying DbContext for transaction management only.
        var dbCtx = (Microsoft.EntityFrameworkCore.DbContext)_context;
        var tx = await dbCtx.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // ----------------------------------------------------------------------------
            // Acquire an exclusive row-level lock on the source account through the
            // infrastructure service. This is the pivot of the concurrency fix – every
            // transfer targeting the same source account will be serialised here.
            // ----------------------------------------------------------------------------
            await _accountLockService.AcquireRowLockAsync(request.SourceAccountId, cancellationToken);

            var sourceAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken);

            var destinationAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken);

            if (sourceAccount is null) throw new Exception("Source not found.");
            if (destinationAccount is null) throw new Exception("Destination not found.");

            // ----------------------------------------------------------------------------
            // Account ownership verification – only transfer from an account you own.
            // ----------------------------------------------------------------------------
            var currentUserId = _requestContext.GetUserId();
            if (sourceAccount.OwnerUserId != Guid.Empty && sourceAccount.OwnerUserId != currentUserId)
                throw new UnauthorizedAccessException("You do not own the source account.");

            // STRICT BALANCE CALCULATION (Credits - Debits)
            var credits = await _context.LedgerEntries
                .Where(e => e.AccountId == request.SourceAccountId && e.Direction == EntryDirection.Credit)
                .SumAsync(e => e.Value.Amount, cancellationToken);

            var debits = await _context.LedgerEntries
                .Where(e => e.AccountId == request.SourceAccountId && e.Direction == EntryDirection.Debit)
                .SumAsync(e => Math.Abs(e.Value.Amount), cancellationToken);

            var currentBalance = credits - debits;

            var principal = request.Amount;
            var systemFee = 1.50m;
            var zimraTax = 0.50m;
            var feeAmount = systemFee + zimraTax;

            if (currentBalance < (principal + feeAmount))
                throw new System.InvalidOperationException("FATAL: Insufficient funds.");

            sourceAccount.MarkActivity();

            var metadata = new AuditMetadata(
                _requestContext.GetUserId(),
                _requestContext.GetIpAddress(),
                _requestContext.GetDeviceId());

            var transaction = new LedgerTransaction(
                Guid.NewGuid(),
                $"REF-{Guid.NewGuid():N}",
                TransactionType.PeerToPeer,
                Guid.NewGuid().ToString(),
                metadata
            );

            // STRICT 4-LEG ROUTING MATRIX (DEBITS ARE NEGATIVE)
            transaction.AddEntry(new LedgerEntry(
                Guid.NewGuid(), transaction.Id, request.SourceAccountId,
                new Money(-principal, request.Currency), EntryDirection.Debit));

            transaction.AddEntry(new LedgerEntry(
                Guid.NewGuid(), transaction.Id, request.DestinationAccountId,
                new Money(principal, request.Currency), EntryDirection.Credit));

            transaction.AddEntry(new LedgerEntry(
                Guid.NewGuid(), transaction.Id, request.SourceAccountId,
                new Money(-feeAmount, request.Currency), EntryDirection.Debit));

            transaction.AddEntry(new LedgerEntry(
                Guid.NewGuid(), transaction.Id, Guid.Parse("33333333-3333-3333-3333-333333333333"),
                new Money(feeAmount, request.Currency), EntryDirection.Credit));

            transaction.Post();

            _context.LedgerTransactions.Add(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return transaction.Id;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await tx.DisposeAsync();
        }
    }
}
