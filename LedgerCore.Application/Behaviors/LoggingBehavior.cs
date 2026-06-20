using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LedgerCore.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Ledger Execution Started: {RequestName}", requestName);
        
        var timer = Stopwatch.StartNew();
        try
        {
            var response = await next();
            timer.Stop();
            _logger.LogInformation("Ledger Execution Completed: {RequestName} in {ElapsedMilliseconds}ms", requestName, timer.ElapsedMilliseconds);
            return response;
        }
        catch (System.Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Ledger Execution Failed: {RequestName} after {ElapsedMilliseconds}ms", requestName, timer.ElapsedMilliseconds);
            throw;
        }
    }
}
