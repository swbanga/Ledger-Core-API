using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> TransferFunds([FromBody] TransferFundsCommand command, CancellationToken cancellationToken)
    {
        var transactionId = await _sender.Send(command, cancellationToken);
        return Ok(new { TransactionId = transactionId });
    }
}
