using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.ProvisionAgentFloat
{
    public class ProvisionAgentFloatCommand : IRequest<Guid>
    {
        public Guid AgentAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
