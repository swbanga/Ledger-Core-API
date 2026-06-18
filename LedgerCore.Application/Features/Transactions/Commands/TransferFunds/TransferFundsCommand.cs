using MediatR;

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public record TransferFundsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency
) : IRequest<Guid>;
