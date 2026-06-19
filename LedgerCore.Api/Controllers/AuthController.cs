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
        var token = _jwtProvider.GenerateToken(request.UserId, request.Role);
        return Ok(new { Token = token });
    }
}

public record TokenRequest(Guid UserId, string Role);
