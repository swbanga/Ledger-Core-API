using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Features.Admin.Commands.ProvisionAgentFloat
{
    public sealed class ProvisionAgentFloatCommandHandler : IRequestHandler<ProvisionAgentFloatCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public ProvisionAgentFloatCommandHandler(IApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Guid> Handle(ProvisionAgentFloatCommand request, CancellationToken cancellationToken)
        {
            // Validate that the agent account exists and is an AgentFloat account
            var db = (DbContext)_context;
            var agentAccount = await db.Set<Account>().FindAsync(
                new object?[] { request.AgentAccountId }, cancellationToken);

            if (agentAccount is null)
                throw new InvalidOperationException(
                    $"Agent account {request.AgentAccountId} not found.");

            if (agentAccount.AccountType != AccountType.AgentFloat)
                throw new InvalidOperationException(
                    $"Account {request.AgentAccountId} is not an AgentFloat.");

            // Build the 2-leg float-provisioning transfer
            var transaction = new LedgerTransaction(
                id: Guid.NewGuid(),
                initiatedBy: request.AgentAccountId,
                transactionType: TransactionType.Transfer,
                channel: Channel.Internal,
                description: $"Float provisioning for agent {request.AgentAccountId}"
            );

            // Leg 1: Debit SystemReserve
            transaction.AddEntry(new LedgerEntry
            {
                AccountId = SystemAccountIds.SystemReserve,
                Direction = EntryDirection.Debit,
                Amount = request.Amount,
                Currency = new CurrencyCode("ETB")
            });

            // Leg 2: Credit AgentFloat account
            transaction.AddEntry(new LedgerEntry
            {
                AccountId = request.AgentAccountId,
                Direction = EntryDirection.Credit,
                Amount = request.Amount,
                Currency = new CurrencyCode("ETB")
            });

            transaction.Post();

            db.Set<LedgerTransaction>().Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
