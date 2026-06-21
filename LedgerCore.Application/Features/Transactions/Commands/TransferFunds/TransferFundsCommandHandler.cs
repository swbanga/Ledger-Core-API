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

    public TransferFundsCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Guid> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var sourceAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken);

        var destinationAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken);

        if (sourceAccount is null) throw new Exception("Source not found.");
        if (destinationAccount is null) throw new Exception("Destination not found.");

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

        return transaction.Id;
    }
}
