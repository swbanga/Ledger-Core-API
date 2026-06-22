using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Application.Contracts;
using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.ProvisionAgentFloat
{
    public sealed class ProvisionAgentFloatCommandHandler : IRequestHandler<ProvisionAgentFloatCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IRequestContext _requestContext;

        public ProvisionAgentFloatCommandHandler(IApplicationDbContext context, IRequestContext requestContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        }

        public async Task<Guid> Handle(ProvisionAgentFloatCommand request, CancellationToken cancellationToken)
        {
            // Validate that the agent account exists and is an AgentFloat account
            var agentAccount = await _context.FindAccountAsync(request.AgentAccountId, cancellationToken);

            if (agentAccount is null)
                throw new InvalidOperationException(
                    $"Agent account {request.AgentAccountId} not found.");

            if (agentAccount.AccountType != AccountType.AgentFloat)
                throw new InvalidOperationException(
                    $"Account {request.AgentAccountId} is not an AgentFloat.");

            var metadata = new AuditMetadata(_requestContext.GetUserId(), _requestContext.GetIpAddress(), _requestContext.GetDeviceId());
            // Build the 2-leg float-provisioning transfer
            var transaction = new LedgerCore.Domain.Entities.LedgerTransaction(Guid.NewGuid(), $"REF-{Guid.NewGuid():N}", LedgerCore.Domain.Enums.TransactionType.PeerToPeer, Guid.NewGuid().ToString(), metadata);

            // Leg 1: Debit System Reserve
            transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
                Guid.NewGuid(),
                transaction.Id,
                LedgerCore.Domain.Constants.SystemAccountIds.SystemReserve,
                new Money(-request.Amount, "USD"),
                LedgerCore.Domain.Enums.EntryDirection.Debit));

            // Leg 2: Credit Agent Float
            transaction.AddEntry(new LedgerCore.Domain.Entities.LedgerEntry(
                Guid.NewGuid(),
                transaction.Id,
                request.AgentAccountId,
                new Money(request.Amount, "USD"),
                LedgerCore.Domain.Enums.EntryDirection.Credit));

            transaction.Post();

            await _context.AddTransactionAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
