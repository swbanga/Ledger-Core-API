using System;

namespace LedgerCore.Application.Authentication;

public interface IJwtProvider
{
    string GenerateToken(Guid userId, string role);
}
