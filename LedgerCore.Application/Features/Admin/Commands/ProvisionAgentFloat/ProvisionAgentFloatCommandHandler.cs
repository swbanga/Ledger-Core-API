using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
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
            var transaction = new LedgerCore.Domain.Entities.LedgerTransaction(Guid.NewGuid(), $"REF-{Guid.NewGuid():N}", LedgerCore.Domain.Enums.TransactionType.PeerToPeer, Guid.NewGuid().ToString());

            // Leg 1: Debit System Reserve
            transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
                Guid.NewGuid(),
                transaction.Id,
                LedgerCore.Domain.Constants.SystemAccountIds.SystemReserve,
                -request.Amount,
                LedgerCore.Domain.Enums.EntryDirection.Debit));

            // Leg 2: Credit Agent Float
            transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
                Guid.NewGuid(),
                transaction.Id,
                request.AgentAccountId,
                request.Amount,
                LedgerCore.Domain.Enums.EntryDirection.Credit));

            transaction.Post();

            db.Set<LedgerTransaction>().Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
