using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Features.Admin.Commands.RebuildProjections;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ReadModels;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LedgerCore.IntegrationTests;

[Trait("Category", "Recovery")]
public class RecoveryTests : IClassFixture<SqlEdgeFixture>
{
    private readonly SqlEdgeFixture _fixture;

    public RecoveryTests(SqlEdgeFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<LedgerDbContext> GetDbContext()
    {
        return await Task.FromResult(_fixture.CreateDbContext());
    }

    private async Task<Guid> CreateAccountAsync(LedgerDbContext context, string number, Guid ownerId)
    {
        var id = Guid.NewGuid();
        var sql = "INSERT INTO [Accounts] (Id, AccountNumber, OwnerUserId, LastActivityUtc, AccountType, KycTier) VALUES ({0}, {1}, {2}, GETUTCDATE(), 1, 1)";
        await context.Database.ExecuteSqlRawAsync(sql, id, number, ownerId);
        return id;
    }

    private async Task PostTransactionAsync(
        LedgerDbContext context,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string? reference = null)
    {
        var txId = Guid.NewGuid();
        var referenceCode = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var ipAddress = "127.0.0.1";
        var deviceId = "TEST-DEVICE";

        var sqlTx = "INSERT INTO [LedgerTransactions] (Id, ReferenceCode, TransactionType, Status, TimestampUtc, CorrelationId, Audit_UserId, Audit_IpAddress, Audit_DeviceId) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})";
        await context.Database.ExecuteSqlRawAsync(sqlTx, txId, referenceCode, 0, (int)TransactionStatus.Posted, DateTime.UtcNow, Guid.NewGuid(), userId, ipAddress, deviceId);

        var debitId = Guid.NewGuid();
        var sqlEntry = "INSERT INTO [LedgerEntries] (Id, TransactionId, AccountId, Direction, Amount, Currency) VALUES ({0}, {1}, {2}, {3}, {4}, {5})";
        await context.Database.ExecuteSqlRawAsync(sqlEntry, debitId, txId, sourceAccountId, (int)EntryDirection.Debit, amount, "EUR");

        var creditId = Guid.NewGuid();
        await context.Database.ExecuteSqlRawAsync(sqlEntry, creditId, txId, destinationAccountId, (int)EntryDirection.Credit, amount, "EUR");
    }

    [Fact]
    public async Task Rebuild_AfterProjectionLoss_RestoresCorrectBalances()
    {
        // Arrange
        await using var context = await GetDbContext();
        var acc1Id = await CreateAccountAsync(context, "REC0-001", Guid.NewGuid());
        var acc2Id = await CreateAccountAsync(context, "REC0-002", Guid.NewGuid());

        await PostTransactionAsync(context, acc1Id, acc2Id, 100m);
        await PostTransactionAsync(context, acc2Id, acc1Id, 30m);

        // expected balances after replay (starting from 0)
        const decimal expected1 = -70m; // -100 + 30
        const decimal expected2 = 70m;  // 100 - 30

        // wipe projection store
        var balanceStore = context.Set<AccountBalanceState>();
        var existing = await balanceStore.ToListAsync();
        balanceStore.RemoveRange(existing);
        await context.SaveChangesAsync();

        var sut = new ProjectionRebuildService(context);

        // Act
        _ = await sut.RebuildAsync(CancellationToken.None);

        // Assert
        var rebuiltStates = await context.Set<AccountBalanceState>().ToListAsync();
        var dict = rebuiltStates.ToDictionary(x => x.AccountId, x => x.CurrentBalance);

        Assert.Equal(expected1, dict[acc1Id]);
        Assert.Equal(expected2, dict[acc2Id]);
    }

    [Fact]
    public async Task Rebuild_RunTwice_ProducesDeterministicResult()
    {
        // Arrange
        await using var context = await GetDbContext();
        var accAId = await CreateAccountAsync(context, "REC0-003", Guid.NewGuid());
        var accBId = await CreateAccountAsync(context, "REC0-004", Guid.NewGuid());

        await PostTransactionAsync(context, accAId, accBId, 200m);
        await PostTransactionAsync(context, accBId, accAId, 50m);

        const decimal expectedA = -150m;
        const decimal expectedB = 150m;

        // ensure clean projection store before first rebuild
        var balanceStore = context.Set<AccountBalanceState>();
        var existing = await balanceStore.ToListAsync();
        balanceStore.RemoveRange(existing);
        await context.SaveChangesAsync();

        var sut = new ProjectionRebuildService(context);

        // Act / first rebuild
        _ = await sut.RebuildAsync(CancellationToken.None);

        // second rebuild
        _ = await sut.RebuildAsync(CancellationToken.None);

        // Assert — final balances identical after two runs
        var finalStates = await context.Set<AccountBalanceState>().ToListAsync();
        var dict = finalStates.ToDictionary(x => x.AccountId, x => x.CurrentBalance);

        Assert.Equal(expectedA, dict[accAId]);
        Assert.Equal(expectedB, dict[accBId]);
    }

    [Fact]
    public async Task Rebuild_AfterReplay_NoDuplicateFinancialState()
    {
        // Arrange
        await using var context = await GetDbContext();
        var acc1Id = await CreateAccountAsync(context, "REC0-005", Guid.NewGuid());
        var acc2Id = await CreateAccountAsync(context, "REC0-006", Guid.NewGuid());

        await PostTransactionAsync(context, acc1Id, acc2Id, 500m);
        await PostTransactionAsync(context, acc2Id, acc1Id, 250m);

        const decimal expected1 = -250m;
        const decimal expected2 = 250m;

        var balanceStore = context.Set<AccountBalanceState>();
        var existing = await balanceStore.ToListAsync();
        balanceStore.RemoveRange(existing);
        await context.SaveChangesAsync();

        var sut = new ProjectionRebuildService(context);

        // Act — replay 5 times
        for (var i = 0; i < 5; i++)
        {
            _ = await sut.RebuildAsync(CancellationToken.None);
        }

        // Assert
        var finalStates = await context.Set<AccountBalanceState>().ToListAsync();
        var dict = finalStates.ToDictionary(x => x.AccountId, x => x.CurrentBalance);

        Assert.Equal(expected1, dict[acc1Id]);
        Assert.Equal(expected2, dict[acc2Id]);
    }
}
