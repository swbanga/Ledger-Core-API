using MediatR;
using LedgerCore.Application.Behaviors;

namespace LedgerCore.Application.Features.Admin.Commands.ProvisionAgentFloat
{
    public class ProvisionAgentFloatCommand : IRequest<Guid>, IIdempotentCommand<Guid>
    {
        public Guid AgentAccountId { get; set; }
        public decimal Amount { get; set; }
        public Guid IdempotencyKey { get; set; }
    }
}
