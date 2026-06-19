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
    [Authorize(Roles = "SysAdmin,Agent,User")]
    public async Task<IActionResult> TransferFunds([FromBody] TransferFundsCommand command, CancellationToken cancellationToken)
    {
        // Extract the required Idempotency Key from the HTTP Headers
        if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKeyValues) || 
            !Guid.TryParse(idempotencyKeyValues.ToString(), out var idempotencyKey))
        {
            return BadRequest("Missing or invalid X-Idempotency-Key header.");
        }

        // Rebuild the command to inject the Key
        var idempotentCommand = command with { IdempotencyKey = idempotencyKey };

        var transactionId = await _sender.Send(idempotentCommand, cancellationToken);
        
        return Ok(new { TransactionId = transactionId });
    }
}
