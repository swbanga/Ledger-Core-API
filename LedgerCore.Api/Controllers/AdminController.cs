using System.Threading.Tasks;
using LedgerCore.Application.Features.Admin.Commands.ProvisionAgentFloat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "SysAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("provision-float")]
        public async Task<IActionResult> ProvisionFloat([FromBody] ProvisionAgentFloatCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { id = result });
        }
    }
}
