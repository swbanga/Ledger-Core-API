using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed class AuditMetadata
{
    public Guid UserId { get; }
    public string IpAddress { get; } = null!;
    public string DeviceId { get; } = null!;

    public AuditMetadata(Guid userId, string ipAddress, string deviceId)
    {
        UserId = userId;
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    }

    // EF Core / serialization constructor
    private AuditMetadata() { }
}
