using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed record AuditMetadata
{
    public Guid UserId { get; init; }
    public string IpAddress { get; init; }
    public string DeviceId { get; init; }

    public AuditMetadata(Guid userId, string ipAddress, string deviceId)
    {
        UserId = userId;
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    }

    // EF Core / serialization constructor
    private AuditMetadata()
    {
        IpAddress = null!;
        DeviceId = null!;
    }
}
