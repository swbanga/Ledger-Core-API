using System;
using System.Threading.Tasks;
using Testcontainers.SqlEdge;
using Microsoft.EntityFrameworkCore;
using LedgerCore.Infrastructure.Database;
using Xunit;

namespace LedgerCore.IntegrationTests;

public class SqlEdgeFixture : IAsyncLifetime
{
    private SqlEdgeContainer _container = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _container = new SqlEdgeBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
            .Build();

        await _container.StartAsync();

        _connectionString = _container.GetConnectionString() + ";Database=LedgerIntegrationTest";

        // Physical schema burn
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Returns a fresh <see cref="LedgerDbContext"/> connected to the ephemeral container.
    /// The database schema is already applied during <see cref="InitializeAsync"/>.
    /// </summary>
    public LedgerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new LedgerDbContext(options);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
