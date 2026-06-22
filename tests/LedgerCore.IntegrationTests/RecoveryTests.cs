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
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Services;
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

    private async Task<Account> CreateAccountAsync(LedgerDbContext context, string number)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = AccountNumber.CreateUserAccount(number),
            Type = AccountType.User
        };
        context.Accounts!.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    private async Task PostTransactionAsync(
        LedgerDbContext context,
        Account source,
        Account destination,
        decimal amount,
        string? reference = null)
    {
        var tx = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            Status = TransactionStatus.Posted,
            TimestampUtc = DateTime.UtcNow
        };

        var debitEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            AccountId = source.Id,
            Direction = EntryDirection.Debit,
            Value = new MonetaryAmount(amount, "EUR"),
            Description = reference ?? "recovery-test"
        };

        var creditEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            AccountId = destination.Id,
            Direction = EntryDirection.Credit,
            Value = new MonetaryAmount(amount, "EUR"),
            Description = reference ?? "recovery-test"
        };

        tx.AddEntry(debitEntry);
        tx.AddEntry(creditEntry);

        context.LedgerTransactions!.Add(tx);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Rebuild_AfterProjectionLoss_RestoresCorrectBalances()
    {
        // Arrange
        await using var context = await GetDbContext();
        var acc1 = await CreateAccountAsync(context, "REC0-001");
        var acc2 = await CreateAccountAsync(context, "REC0-002");

        await PostTransactionAsync(context, acc1, acc2, 100m);
        await PostTransactionAsync(context, acc2, acc1, 30m);

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
        var result = await sut.RebuildAsync(CancellationToken.None);

        // Assert
        var rebuiltStates = await context.Set<AccountBalanceState>().ToListAsync();
        var dict = rebuiltStates.ToDictionary(x => x.AccountId, x => x.CurrentBalance);

        Assert.Equal(expected1, dict[acc1.Id]);
        Assert.Equal(expected2, dict[acc2.Id]);
        Assert.Equal(2, result.TotalAccountsRebuilt);
        Assert.Equal(2, result.TotalTransactionsProcessed);
    }

    [Fact]
    public async Task Rebuild_RunTwice_ProducesDeterministicResult()
    {
        // Arrange
        await using var context = await GetDbContext();
        var accA = await CreateAccountAsync(context, "REC0-003");
        var accB = await CreateAccountAsync(context, "REC0-004");

        await PostTransactionAsync(context, accA, accB, 200m);
        await PostTransactionAsync(context, accB, accA, 50m);

        const decimal expectedA = -150m;
        const decimal expectedB = 150m;

        // ensure clean projection store before first rebuild
        var balanceStore = context.Set<AccountBalanceState>();
        var existing = await balanceStore.ToListAsync();
        balanceStore.RemoveRange(existing);
        await context.SaveChangesAsync();

        var sut = new ProjectionRebuildService(context);

        // Act / first rebuild
        var result1 = await sut.RebuildAsync(CancellationToken.None);

        // second rebuild
        var result2 = await sut.RebuildAsync(CancellationToken.None);

        // Assert — final balances identical after two runs
        var finalStates = await context.Set<AccountBalanceState>().ToListAsync();
        var dict = finalStates.ToDictionary(x => x.AccountId, x => x.CurrentBalance);

        Assert.Equal(expectedA, dict[accA.Id]);
        Assert.Equal(expectedB, dict[accB.Id]);

        // instance‑level properties are also identical
        Assert.Equal(result1.TotalAccountsRebuilt, result2.TotalAccountsRebuilt);
        Assert.Equal(result1.TotalTransactionsProcessed, result2.TotalTransactionsProcessed);
    }

    [Fact]
    public async Task Rebuild_AfterReplay_NoDuplicateFinancialState()
    {
        // Arrange
        await using var context = await GetDbContext();
        var acc1 = await CreateAccountAsync(context, "REC0-005");
        var acc2 = await CreateAccountAsync(context, "REC0-006");

        await PostTransactionAsync(context, acc1, acc2, 500m);
        await PostTransactionAsync(context, acc2, acc1, 250m);

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

        Assert.Equal(expected1, dict[acc1.Id]);
        Assert.Equal(expected2, dict[acc2.Id]);
    }
}
