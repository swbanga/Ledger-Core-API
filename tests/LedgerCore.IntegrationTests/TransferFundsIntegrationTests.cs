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

        var userId = reqCtx.GetUserId();

        var sourceAccount = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = userId
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

        var userId = reqCtx.GetUserId();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = userId
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

        var userId = reqCtx.GetUserId();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = userId
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

        var userId = reqCtx.GetUserId();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            KycTier = KycTier.Tier1,   // tier 1 has a default limit of 10 000
            OwnerUserId = userId
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
        // Isolate state within a dedicated scope
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var floatId = Guid.NewGuid();

        var userId = reqCtx.GetUserId();
        var sourceAccount = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount("0999999991"),
            AccountType = AccountType.User,
            OwnerUserId = userId
        };
        var destinationAccount = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount("0999999992"),
            AccountType = AccountType.User,
            OwnerUserId = userId
        };
        var floatAccount = new Account
        {
            Id = floatId,
            AccountNumber = AccountNumber.CreateSystemAccount("FLOAT"),
            AccountType = LedgerCore.Domain.Enums.AccountType.SystemRevenue,
            OwnerUserId = userId
        };

        context.Accounts.AddRange(sourceAccount, destinationAccount, floatAccount);

        var audit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var depositTx = new LedgerTransaction(
            Guid.NewGuid(),
            "INIT",
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            audit);

        var creditEntry = new LedgerEntry(
            Guid.NewGuid(),
            depositTx.Id,
            sourceId,
            new Money(104m, "USD"),
            EntryDirection.Credit);
        var debitEntry = new LedgerEntry(
            Guid.NewGuid(),
            depositTx.Id,
            floatId,
            new Money(-104m, "USD"),
            EntryDirection.Debit);

        depositTx.AddEntry(creditEntry);
        depositTx.AddEntry(debitEntry);
        depositTx.Post();

        context.LedgerTransactions.Add(depositTx);
        context.LedgerEntries.AddRange(creditEntry, debitEntry);

        await context.SaveChangesAsync();

        int succeeded = 0;
        int failedOrConflict = 0;
        var lockObj = new object();

        await Parallel.ForEachAsync(Enumerable.Range(0, 10), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (i, ct) =>
        {
            await using var innerScope = _fixture.Services.CreateAsyncScope();
            var scopedSender = innerScope.ServiceProvider.GetRequiredService<MediatR.ISender>();
            var idempotencyKey = Guid.NewGuid();
            var command = new TransferFundsCommand(sourceId, destId, 102m, "USD", idempotencyKey);
            try
            {
                await scopedSender.Send(command, ct);
                lock (lockObj) succeeded++;
            }
            catch (InvalidOperationException)
            {
                lock (lockObj) failedOrConflict++;
            }
            catch (DbUpdateConcurrencyException)
            {
                lock (lockObj) failedOrConflict++;
            }
            catch (InsufficientFundsException)
            {
                lock (lockObj) failedOrConflict++;
            }
            catch (Exception)
            {
                lock (lockObj) failedOrConflict++;
            }
        });

        var credits = await context.LedgerEntries
            .Where(e => e.AccountId == sourceId && e.Direction == EntryDirection.Credit)
            .SumAsync(e => Math.Abs(e.Value.Amount));
        var debits = await context.LedgerEntries
            .Where(e => e.AccountId == sourceId && e.Direction == EntryDirection.Debit)
            .SumAsync(e => Math.Abs(e.Value.Amount));
        Assert.Equal(0m, credits - debits);
        Assert.True(succeeded == 1 && failedOrConflict >= 9, $"Succeeded={succeeded}, Failures/Conflicts={failedOrConflict}");
    }

    private static string GenerateUniqueAccountNumber()
    {
        return $"0{System.Random.Shared.Next(100000000, 999999999)}";
    }

    [Fact]
    public async Task TransferFunds_ShouldSucceed_WhenOwnerTransfersOwnFunds()
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

        var userId = reqCtx.GetUserId();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = userId
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var audit = new AuditMetadata(userId, reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(
            Guid.NewGuid(),
            GenerateUniqueReference("OWNEROPEN"),
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            audit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(
            Guid.NewGuid(),
            openingTx.Id,
            sourceId,
            new Money(200m, "USD"),
            EntryDirection.Credit));
        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 50m, "USD", Guid.NewGuid());

        // Act
        var txId = await sender.Send(command, CancellationToken.None);

        // Assert
        var entries = await context.LedgerEntries
            .Where(e => e.TransactionId == txId)
            .ToListAsync();
        Assert.NotEmpty(entries);
    }

    [Fact]
    public async Task TransferFunds_ShouldThrowUnauthorized_WhenRequesterDoesNotOwnSourceAccount()
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

        // OwnerUserId is set to a guid that does NOT match the current user.
        var differentOwnerId = Guid.NewGuid();
        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = differentOwnerId
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var audit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(
            Guid.NewGuid(),
            GenerateUniqueReference("OTHEROWNER"),
            TransactionType.PeerToPeer,
            Guid.NewGuid().ToString(),
            audit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(
            Guid.NewGuid(),
            openingTx.Id,
            sourceId,
            new Money(100m, "USD"),
            EntryDirection.Credit));
        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 10m, "USD", Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sender.Send(command, CancellationToken.None));
    }

    [Fact]
    public async Task TransferFunds_ShouldThrowConcurrencyException_WhenOptimisticConflict()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccountNumber = GenerateUniqueAccountNumber();
        var destAccountNumber = GenerateUniqueAccountNumber();

        var userId = reqCtx.GetUserId();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount(sourceAccountNumber),
            AccountType = AccountType.User,
            OwnerUserId = userId
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount(destAccountNumber),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        // Seed source with some funds
        var openingAudit = new AuditMetadata(userId, reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
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
            new Money(500m, "USD"),
            EntryDirection.Credit));
        await context.SaveChangesAsync();

        // Load the source account in a second context that will be the victim of external delete.
        await using var innerScope = _fixture.Services.CreateAsyncScope();
        var context2 = innerScope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var sourceFromC2 = await context2.Accounts.FindAsync(sourceId);
        Assert.NotNull(sourceFromC2);

        // Update the tracked entity (mark activity)
        sourceFromC2!.MarkActivity();

        // Externally delete the row using raw SQL to simulate concurrent removal
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Accounts] WHERE Id = {0}", sourceId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
        Assert.Contains("optimistic", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateUniqueReference(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
