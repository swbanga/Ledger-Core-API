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
    private readonly TimeProvider _timeProvider;
    private readonly IRequestContext _requestContext;

    public TransferFundsCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IRequestContext requestContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _requestContext = requestContext;
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
            .SumAsync(e => e.Value.Amount, cancellationToken);

        var principal = request.Amount;
        var systemFee = 1.50m;
        var zimraTax = 0.50m;
        var totalDebit = principal + systemFee + zimraTax;

        if (currentBalance < request.Amount)
            throw new System.InvalidOperationException("FATAL: Insufficient funds.");
        // -----------------------------------------------------

        // Mark the source account as modified to enforce optimistic concurrency
        sourceAccount.MarkActivity();

        var transactionId = Guid.NewGuid();

        // --------------------------------------------------------
        // NEW STRICTLY CONSTRUCTED 4‑LEG ROUTING MATRIX USING DOMAIN ADDENTRY
        // --------------------------------------------------------

        var metadata = new AuditMetadata(_requestContext.GetUserId(), _requestContext.GetIpAddress(), _requestContext.GetDeviceId());

        var transaction = new LedgerCore.Domain.Entities.LedgerTransaction(
            transactionId,
            $"REF-{Guid.NewGuid():N}",
            LedgerCore.Domain.Enums.TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(), // Generates a unique CorrelationId for distributed tracing
            metadata
        );

        var currency = request.Currency;
        var feeAmount = systemFee + zimraTax;

        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(),
            transaction.Id,
            request.SourceAccountId,
            new Money(-request.Amount, request.Currency),
            LedgerCore.Domain.Enums.EntryDirection.Debit));

        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(),
            transaction.Id,
            request.DestinationAccountId,
            new Money(request.Amount, request.Currency),
            LedgerCore.Domain.Enums.EntryDirection.Credit));

        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(),
            transaction.Id,
            request.SourceAccountId,
            new Money(-feeAmount, request.Currency),
            LedgerCore.Domain.Enums.EntryDirection.Debit));

        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(),
            transaction.Id,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            new Money(feeAmount, request.Currency),
            LedgerCore.Domain.Enums.EntryDirection.Credit));

        // The Absolute Mathematical Invariant
        // Mathematically locks the transaction and transitions state to Posted
        transaction.Post(); 

        _context.LedgerTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
