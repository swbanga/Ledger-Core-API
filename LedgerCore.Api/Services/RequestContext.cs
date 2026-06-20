using System;
using System.Linq;
using System.Security.Claims;
using LedgerCore.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace LedgerCore.Api.Services;

public sealed class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null) return Guid.Empty;

        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? user.FindFirstValue("sub");

        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    public string GetIpAddress()
    {
        var remoteIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        return remoteIp ?? "0.0.0.0";
    }

    public string GetDeviceId()
    {
        var headers = _httpContextAccessor.HttpContext?.Request?.Headers;
        if (headers is null) return "unknown";

        const string headerName = "X-Device-Id";
        if (headers.TryGetValue(headerName, out var values))
        {
            var raw = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(raw)) return raw;
        }

        return "unknown";
    }
}
