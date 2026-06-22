using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.RebuildProjections;

public class RebuildProjectionsCommandHandler : IRequestHandler<RebuildProjectionsCommand, RebuildProjectionsResult>
{
    private readonly IProjectionRebuildService _rebuildService;

    public RebuildProjectionsCommandHandler(IProjectionRebuildService rebuildService)
    {
        _rebuildService = rebuildService;
    }

    public async Task<RebuildProjectionsResult> Handle(RebuildProjectionsCommand request, CancellationToken cancellationToken)
    {
        return await _rebuildService.RebuildAsync(cancellationToken);
    }
}
