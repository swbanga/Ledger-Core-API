using FluentValidation;

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public class TransferFundsValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty().WithMessage("Source account ID must not be empty.");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty().WithMessage("Destination account ID must not be empty.");

        RuleFor(x => x)
            .Must(x => x.SourceAccountId != x.DestinationAccountId)
            .WithMessage("Source and destination accounts must be different.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency must not be empty.")
            .Length(3).WithMessage("Currency must be exactly 3 characters.");
    }
}
