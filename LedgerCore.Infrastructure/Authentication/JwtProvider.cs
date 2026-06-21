using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LedgerCore.Application.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LedgerCore.Infrastructure.Authentication;

public sealed class JwtProvider : IJwtProvider
{
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtProvider(IOptions<JwtSettings> jwtOptions, TimeProvider timeProvider)
    {
        _jwtSettings = jwtOptions.Value;
        _timeProvider = timeProvider;

        if (string.IsNullOrEmpty(_jwtSettings.Secret) || _jwtSettings.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters long.");
        }
    }

    public string GenerateToken(Guid userId, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            null,
            _timeProvider.GetUtcNow().AddMinutes(_jwtSettings.ExpirationTimeInMinutes).UtcDateTime,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
