using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace LedgerCore.Infrastructure.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;

    public RedisHealthCheck(IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            return HealthCheckResult.Healthy("Redis is not configured (skip).");
        }

        try
        {
            var latency = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis is reachable (ping {latency.TotalMilliseconds:F2} ms).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connectivity failure.", ex);
        }
    }
}
