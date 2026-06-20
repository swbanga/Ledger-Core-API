using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;

namespace LedgerCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LedgerCore.Application.Behaviors.VelocityCheckBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LedgerCore.Application.Behaviors.IdempotencyBehavior<,>));
        return services;
    }
}
