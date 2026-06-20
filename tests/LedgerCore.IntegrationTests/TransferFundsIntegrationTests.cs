using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Data;
using LedgerCore.Application.Features.Transactions.Commands.TransferFunds;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LedgerCore.IntegrationTests;

public class TransferFundsIntegrationTests : IClassFixture<SqlEdgeFixture>
{
    private readonly SqlEdgeFixture _fixture;

    public TransferFundsIntegrationTests(SqlEdgeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TransferFunds_ShouldUpdateBalancesAndMaintainInvariant_WhenValid()
    {
        // ── Arrange ───────────────────────────────────────────────
        await using var context = _fixture.CreateDbContext();

        // Fake dependencies
        var fakeRequestContext = new FakeRequestContext();
        var fakeUnitOfWork = new FakeUnitOfWork(context);
        var timeProvider = TimeProvider.System;

        // Seed source & destination accounts
        var sourceAccount = new Account
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            AccountNumber = AccountNumber.CreateUserAccount("SRC10001"),
            Currency = "USD",
            Type = AccountType.User
        };
        var destinationAccount = new Account
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            AccountNumber = AccountNumber.CreateUserAccount("DST20001"),
            Currency = "USD",
            Type = AccountType.User
        };
        context.Accounts.Add(sourceAccount);
        context.Accounts.Add(destinationAccount);

        // Give source account a starting balance (CID approach)
        var openingAudit = new AuditMetadata(
            fakeRequestContext.GetUserId(),
            fakeRequestContext.GetIpAddress(),
            fakeRequestContext.GetDeviceId());
        var openingTransaction = new LedgerTransaction(
            Guid.NewGuid(),
            "OPENING",
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            openingAudit);
        context.LedgerTransactions.Add(openingTransaction);
        context.LedgerEntries.Add(new LedgerEntry(
            Guid.NewGuid(),
            openingTransaction.Id,
            sourceAccount.Id,
            new Money(1000m, "USD"),
            EntryDirection.Credit));

        await context.SaveChangesAsync();

        // Handler under test
        var handler = new TransferFundsCommandHandler(
            context,
            fakeUnitOfWork,
            timeProvider,
            fakeRequestContext);

        var command = new TransferFundsCommand
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destinationAccount.Id,
            Amount = 100m,
            Currency = "USD",
            IdempotencyKey = Guid.NewGuid()
        };

        // ── Act ───────────────────────────────────────────────────
        var transactionId = await handler.Handle(command, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────
        var entries = await context.LedgerEntries
            .Where(e => e.TransactionId == transactionId)
            .ToListAsync();

        // Source debited (principal + fee + tax)
        var sourceEntry = entries.Single(e => e.AccountId == sourceAccount.Id);
        Assert.Equal(EntryDirection.Debit, sourceEntry.Direction);
        Assert.Equal(-101.50m, sourceEntry.Value.Amount);   // 100 + 1.50 + 0.50

        // Destination credited (principal)
        var destEntry = entries.Single(e => e.AccountId == destinationAccount.Id);
        Assert.Equal(EntryDirection.Credit, destEntry.Direction);
        Assert.Equal(100m, destEntry.Value.Amount);

        // System fee routed
        var feeEntry = entries.Single(e => e.AccountId == SystemAccountIds.SystemRevenue);
        Assert.Equal(EntryDirection.Credit, feeEntry.Direction);
        Assert.Equal(1.50m, feeEntry.Value.Amount);

        // Tax routed
        var taxEntry = entries.Single(e => e.AccountId == SystemAccountIds.TaxLiabilityZimra);
        Assert.Equal(EntryDirection.Credit, taxEntry.Direction);
        Assert.Equal(0.50m, taxEntry.Value.Amount);

        // Absolute invariant ∑ = 0
        var sum = entries.Sum(e => e.Value.Amount);
        Assert.Equal(0m, sum);
    }

    // ── Fakes inside the test assembly ────────────────────────────

    private sealed class FakeRequestContext : IRequestContext
    {
        public Guid GetUserId() =>
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        public string GetIpAddress() => "127.0.0.1";

        public string GetDeviceId() => "test-device";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly IApplicationDbContext _dbContext;

        public FakeUnitOfWork(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            // Downcast to the EF Core context because the interface only exposes application-level contract.
            return ((LedgerDbContext)_dbContext).SaveChangesAsync(cancellationToken);
        }
    }
}
