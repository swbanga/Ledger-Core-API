using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LedgerCore.Application.Features.Accounts.Commands.CreateAccount;
using LedgerCore.Application.Features.Accounts.Queries.GetAccountBalance;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountsController : ControllerBase
{
    private readonly ISender _sender;

    public AccountsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "SysAdmin,Agent")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var accountId = await _sender.Send(command);
        return Ok(new { AccountId = accountId });
    }

    [HttpGet("{id:guid}/balance")]
    [Authorize(Roles = "SysAdmin,Agent,User")]
    public async Task<IActionResult> GetBalance(Guid id)
    {
        var query = new GetAccountBalanceQuery(id);
        var balance = await _sender.Send(query);
        
        return Ok(new 
        { 
            AccountId = id, 
            CalculatedBalance = balance 
        });
    }
}