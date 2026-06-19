using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Application.Interfaces;

namespace LedgerCore.Application.Behaviors;

public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    Guid IdempotencyKey { get; }
}

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly IIdempotencyService _idempotencyService;

    public IdempotencyBehavior(IIdempotencyService idempotencyService)
    {
        _idempotencyService = idempotencyService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (await _idempotencyService.RequestExistsAsync(request.IdempotencyKey))
        {
            throw new LedgerCore.Application.Exceptions.IdempotencyException($"Idempotency check failed. Command {request.IdempotencyKey} has already been processed.");
        }

        await _idempotencyService.CreateRequestAsync(request.IdempotencyKey, typeof(TRequest).Name);

        return await next();
    }
}
