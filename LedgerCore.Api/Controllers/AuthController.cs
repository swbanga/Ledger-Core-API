using Microsoft.AspNetCore.Mvc;
using LedgerCore.Application.Authentication;
using System;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtProvider _jwtProvider;

    public AuthController(IJwtProvider jwtProvider)
    {
        _jwtProvider = jwtProvider;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        // TODO: Replace development token minting with proper identity provider before production deployment.
        var token = _jwtProvider.GenerateToken(request.UserId, "User");
        return Ok(new { Token = token });
    }
}

public record TokenRequest(Guid UserId);
