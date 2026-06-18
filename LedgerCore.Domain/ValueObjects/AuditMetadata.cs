using System;

namespace LedgerCore.Domain.ValueObjects;

public enum Channel
{
    Mobile,
    USSD,
    Web
}

public sealed record AuditMetadata
{
    public Guid CreatedByUserId { get; }
    public string IpAddress { get; }
    public Channel Channel { get; }

    public AuditMetadata(Guid createdByUserId, string ipAddress, Channel channel)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId cannot be empty.", nameof(createdByUserId));

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IpAddress cannot be null or whitespace.", nameof(ipAddress));

        CreatedByUserId = createdByUserId;
        IpAddress = ipAddress;
        Channel = channel;
    }

    public override string ToString() => $"{CreatedByUserId} | {IpAddress} | {Channel}";
}
