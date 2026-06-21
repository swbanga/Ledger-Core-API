using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Features.Transactions.Commands.TransferFunds;
using LedgerCore.Domain.Constants;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
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
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var sourceAccount = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User
        };
        var destinationAccount = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(sourceAccount, destinationAccount);

        // Seed source with $1000
        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(
            Guid.NewGuid(),
            GenerateUniqueReference("OPENING"),
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            openingAudit);

        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(
            Guid.NewGuid(),
            openingTx.Id,
            sourceId,
            new Money(1000m, "USD"),
            EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 100m, "USD", Guid.NewGuid());

        // Act
        var transactionId = await sender.Send(command, CancellationToken.None);

        // Assert
        var entries = await context.LedgerEntries
            .Where(e => e.TransactionId == transactionId)
            .ToListAsync();

        Assert.Equal(4, entries.Count);
        Assert.Equal(0m, entries.Sum(e => e.Value.Amount));
    }

    [Fact]
    public async Task TransferFunds_ShouldThrowInsufficientFunds_WhenBalanceTooLow()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), GenerateUniqueReference("OPENING"), TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(50m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 500m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<System.InvalidOperationException>(() => sender.Send(command, CancellationToken.None));
        Assert.Contains("insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransferFunds_ShouldThrow_WhenAmountIsZero()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), GenerateUniqueReference("OPENING"), TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(1000m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 0m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<System.ArgumentException>(async () => await sender.Send(command));
        Assert.Contains("Amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransferFunds_ShouldThrowKycLimitException_WhenLimitExceeded()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            KycTier = KycTier.Tier1   // tier 1 has a default limit of 10 000
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), GenerateUniqueReference("OPENING"), TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(20_000m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 15_000m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(command, CancellationToken.None));
        Assert.Contains("KYC", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransferFunds_ShouldPreventOverdraft_UnderHighConcurrency_WhenOccTriggers()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var isolatedSourceId = Guid.NewGuid();
        var isolatedDestId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var sourceAccount = new Account
        {
            Id = isolatedSourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User
        };
        var destinationAccount = new Account
        {
            Id = isolatedDestId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        db.Accounts.AddRange(sourceAccount, destinationAccount);

        // Seed exactly $100
        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(
            Guid.NewGuid(),
            GenerateUniqueReference("LOAD"),
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            openingAudit);
        db.LedgerTransactions.Add(openingTx);
        db.LedgerEntries.Add(new LedgerEntry(
            Guid.NewGuid(),
            openingTx.Id,
            isolatedSourceId,
            new Money(100m, "USD"),
            EntryDirection.Credit));

        await db.SaveChangesAsync();

        int succeeded = 0;
        int insufficient = 0;
        int concurrency = 0;
        int other = 0;
        var lockObj = new object();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var range = Enumerable.Range(0, 10);
        await Parallel.ForEachAsync(range, parallelOptions, async (i, ct) =>
        {
            await using var innerScope = _fixture.Services.CreateAsyncScope();
            var scopedSender = innerScope.ServiceProvider.GetRequiredService<MediatR.ISender>();
            // Each iteration MUST use a new IdempotencyKey to bypass Redis cache
            var idempotencyKey = Guid.NewGuid();
            var cmd = new TransferFundsCommand(isolatedSourceId, isolatedDestId, 100m, "USD", idempotencyKey);
            try
            {
                await scopedSender.Send(cmd);
                lock (lockObj) { succeeded++; }
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
            {
                lock (lockObj) { insufficient++; }
            }
            catch (DbUpdateConcurrencyException)
            {
                lock (lockObj) { concurrency++; }
            }
            catch (InsufficientFundsException)
            {
                lock (lockObj) { insufficient++; }
            }
            catch (Exception)
            {
                lock (lockObj) { other++; }
            }
        });

        Assert.Equal(0, other);
        Assert.Equal(1, succeeded);
        Assert.Equal(9, insufficient + concurrency);

        // Verify final balance is $0
        await using var assertScope = _fixture.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sourceEntries = await assertDb.LedgerEntries
            .Where(e => e.AccountId == isolatedSourceId)
            .ToListAsync();
        var balance = sourceEntries.Sum(
            e => e.Direction == EntryDirection.Credit ? e.Value.Amount : -e.Value.Amount);
        Assert.Equal(0m, balance);
    }

    private static string GenerateUniqueAccountNumber()
    {
        return $"0{System.Random.Shared.Next(100000000, 999999999)}";
    }

    private static string GenerateUniqueReference(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
