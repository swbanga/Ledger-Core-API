using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using LedgerCore.Application.Data;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Data.Interceptors;
using LedgerCore.Infrastructure.BackgroundJobs;
using MassTransit;
using LedgerCore.Application.Features.Projections;

namespace LedgerCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<LedgerDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("Database"), sqlOptions => sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null))
                   .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LedgerDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<OutboxProcessorService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "LedgerCore_";
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<BalanceProjectionConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "rabbitmq://localhost");
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
