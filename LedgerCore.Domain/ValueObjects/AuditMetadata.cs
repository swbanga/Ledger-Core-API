using System;
using LedgerCore.Domain.Enums;

namespace LedgerCore.Domain.ValueObjects;

public sealed record AuditMetadata(Guid CreatedByUserId, string IpAddress, Channel Channel);
