using System;
using System.Threading.Tasks;

namespace LedgerCore.Application.Interfaces;

public interface IIdempotencyService
{
    Task<bool> RequestExistsAsync(Guid idempotencyKey);
    Task CreateRequestAsync(Guid idempotencyKey, string commandName);
}
