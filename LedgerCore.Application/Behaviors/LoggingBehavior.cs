using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using LedgerCore.Application.Contracts;
using System.Text.Json;

namespace LedgerCore.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IRequestContext _requestContext;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IRequestContext requestContext)
    {
        _logger = logger;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var userId = _requestContext?.GetUserId().ToString() ?? "anonymous";
        var correlationId = Activity.Current?.TraceId.ToString() ?? string.Empty;
        var spanId = Activity.Current?.SpanId.ToString() ?? string.Empty;

        var requestName = typeof(TRequest).Name;
        var requestReference = JsonSerializer.Serialize(request);
        _logger.LogInformation("Ledger Execution Started: {RequestName} {RequestReference} {UserId} {TraceId} {SpanId}", requestName, requestReference, userId, correlationId, spanId);

        var timer = Stopwatch.StartNew();
        try
        {
            var response = await next();
            timer.Stop();
            _logger.LogInformation("Ledger Execution Completed: {RequestName} in {ElapsedMilliseconds}ms {UserId} {TraceId} {SpanId}", requestName, timer.ElapsedMilliseconds, userId, correlationId, spanId);
            return response;
        }
        catch (System.Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Ledger Execution Failed: {RequestName} after {ElapsedMilliseconds}ms {UserId} {TraceId} {SpanId}", requestName, timer.ElapsedMilliseconds, userId, correlationId, spanId);
            throw;
        }
    }
}
