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

        if (currentBalance < totalDebit)
        {
            throw new LedgerCore.Domain.Exceptions.InsufficientFundsException(
                $"FATAL: Insufficient funds. Account {request.SourceAccountId} holds {currentBalance}, but attempted to transfer {request.Amount} (total including fees/taxes: {totalDebit}).");
        }
        // -----------------------------------------------------

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

        // 1. Leg 1: Source Debit (Principal + Fee + Tax)
        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(), 
            transaction.Id, 
            request.SourceAccountId, 
            new Money(-totalDebit, currency), 
            LedgerCore.Domain.Enums.EntryDirection.Debit));

        // 2. Leg 2: Destination Credit (Principal)
        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(), 
            transaction.Id, 
            request.DestinationAccountId, 
            new Money(principal, currency), 
            LedgerCore.Domain.Enums.EntryDirection.Credit));

        // 3. Leg 3: System Revenue Credit (Platform Fee)
        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(), 
            transaction.Id, 
            SystemAccountIds.SystemRevenue, 
            new Money(systemFee, currency), 
            LedgerCore.Domain.Enums.EntryDirection.Credit));

        // 4. Leg 4: ZIMRA Tax Liability Credit (IMTT)
        transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
            Guid.NewGuid(), 
            transaction.Id, 
            SystemAccountIds.TaxLiabilityZimra, 
            new Money(zimraTax, currency), 
            LedgerCore.Domain.Enums.EntryDirection.Credit));

        // The Absolute Mathematical Invariant
        // Mathematically locks the transaction and transitions state to Posted
        transaction.Post(); 

        _context.LedgerTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
