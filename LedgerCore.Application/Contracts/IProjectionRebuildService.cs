using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Features.Admin.Commands.RebuildProjections;

namespace LedgerCore.Application.Contracts;

public interface IProjectionRebuildService
{
    Task<RebuildProjectionsResult> RebuildAsync(CancellationToken cancellationToken = default);
}
