using LedgerCore.Application.Data;

namespace LedgerCore.Infrastructure.Database;

public class UnitOfWork : IUnitOfWork
{
    private readonly LedgerDbContext _context;

    public UnitOfWork(LedgerDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
