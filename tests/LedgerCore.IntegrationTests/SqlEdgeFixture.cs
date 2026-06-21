using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.SqlEdge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LedgerCore.Application;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Interfaces;
using LedgerCore.Infrastructure;
using LedgerCore.Infrastructure.Database;
using Xunit;

namespace LedgerCore.IntegrationTests;

public class SqlEdgeFixture : IAsyncLifetime
{
    private SqlEdgeContainer _container = null!;
    private string _connectionString = null!;
    private IServiceProvider _serviceProvider = null!;

    public IServiceProvider Services => _serviceProvider;

    public async Task InitializeAsync()
    {
        _container = new SqlEdgeBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
            .Build();

        await _container.StartAsync();

        _connectionString = _container.GetConnectionString() + ";Database=LedgerIntegrationTest";

        // Build the service pipeline
        var services = new ServiceCollection();
        services.AddLogging();

        var inMemorySettings = new Dictionary<string, string>
        {
            { "ConnectionStrings:LedgerCore", _connectionString }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Override any production Redis IDistributedCache with in-memory.
        var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
        if (redisDescriptor != null) { services.Remove(redisDescriptor); }
        services.AddDistributedMemoryCache();

        // Force weld: remove any existing DbContextOptions descriptor and register using the container's dynamic port.
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LedgerDbContext>));
        if (descriptor != null) { services.Remove(descriptor); }

        services.AddDbContext<LedgerDbContext>(options =>
            options.UseSqlServer(_container.GetConnectionString()));

        services.AddSingleton(TimeProvider.System);

        // Override the request context with the testing fake.
        services.AddTransient<IRequestContext, FakeRequestContext>();
        // Bypass idempotency check entirely for integration tests.
        services.AddTransient<IIdempotencyService, FakeIdempotencyService>();

        _serviceProvider = services.BuildServiceProvider();

        // Physical schema burn
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Returns a fresh <see cref="LedgerDbContext"/> connected to the ephemeral container.
    /// The database schema is already applied during <see cref="InitializeAsync"/>.
    /// </summary>
    public LedgerDbContext CreateDbContext()
    {
        // Use the container's connection string via the DI options
        var options = _serviceProvider.GetRequiredService<DbContextOptions<LedgerDbContext>>();
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

internal sealed class FakeRequestContext : IRequestContext
{
    public Guid GetUserId() =>
        Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public string GetIpAddress() => "127.0.0.1";

    public string GetDeviceId() => "TEST-DEVICE";
}

internal sealed class FakeIdempotencyService : IIdempotencyService
{
    public Task<bool> RequestExistsAsync(Guid idempotencyKey) => Task.FromResult(false);
    public Task CreateRequestAsync(Guid idempotencyKey, string commandName) => Task.CompletedTask;
}
