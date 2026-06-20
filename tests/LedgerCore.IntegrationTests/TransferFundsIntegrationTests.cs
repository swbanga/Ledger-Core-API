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
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccount = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount("0123456789"),
            AccountType = AccountType.User
        };
        var destinationAccount = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount("0987654321"),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(sourceAccount, destinationAccount);

        // Seed source with $1000
        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(
            Guid.NewGuid(),
            "OPENING",
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
        var transactionId = await mediator.Send(command, CancellationToken.None);

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
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount("0123456789"),
            AccountType = AccountType.User
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount("0987654321"),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), "OPENING", TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(50m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 500m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InsufficientFundsException>(() => mediator.Send(command, CancellationToken.None));
        Assert.Contains("insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Prove DB untouched beyond the opening transaction
        await using var verifyContext = _fixture.CreateDbContext();
        var txCount = await verifyContext.LedgerTransactions.CountAsync();
        Assert.Equal(1, txCount);
    }

    [Fact]
    public async Task TransferFunds_ShouldThrow_WhenAmountIsZero()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount("0123456789"),
            AccountType = AccountType.User
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount("0987654321"),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), "OPENING", TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(1000m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 0m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command, CancellationToken.None));
        Assert.Contains("Amount", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var verifyContext = _fixture.CreateDbContext();
        var txCount = await verifyContext.LedgerTransactions.CountAsync();
        Assert.Equal(1, txCount);
    }

    [Fact]
    public async Task TransferFunds_ShouldThrowKycLimitException_WhenLimitExceeded()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var reqCtx = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var source = new Account
        {
            Id = sourceId,
            AccountNumber = AccountNumber.CreateUserAccount("0123456789"),
            AccountType = AccountType.User,
            KycTier = KycTier.Tier1   // tier 1 has a default limit of 10 000
        };
        var destination = new Account
        {
            Id = destId,
            AccountNumber = AccountNumber.CreateUserAccount("0987654321"),
            AccountType = AccountType.User
        };

        context.Accounts.AddRange(source, destination);

        var openingAudit = new AuditMetadata(reqCtx.GetUserId(), reqCtx.GetIpAddress(), reqCtx.GetDeviceId());
        var openingTx = new LedgerTransaction(Guid.NewGuid(), "OPENING", TransactionType.PeerToPeer, Guid.NewGuid().ToString(), openingAudit);
        context.LedgerTransactions.Add(openingTx);
        context.LedgerEntries.Add(new LedgerEntry(Guid.NewGuid(), openingTx.Id, sourceId, new Money(20_000m, "USD"), EntryDirection.Credit));

        await context.SaveChangesAsync();

        var command = new TransferFundsCommand(sourceId, destId, 15_000m, "USD", Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(command, CancellationToken.None));
        Assert.Contains("KYC", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var verifyContext = _fixture.CreateDbContext();
        var txCount = await verifyContext.LedgerTransactions.CountAsync();
        Assert.Equal(1, txCount);
    }
}
