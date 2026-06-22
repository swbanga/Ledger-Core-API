using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using LedgerCore.Infrastructure.BackgroundJobs;

namespace LedgerCore.Infrastructure.Health;

public class OutboxProcessorHealthCheck : IHealthCheck
{
    private readonly OutboxProcessorService? _service;

    public OutboxProcessorHealthCheck(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetService<OutboxProcessorService>();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_service is null || !_service.IsRunning)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Outbox processor is not running."));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy("Outbox processor is running."));
    }
}
