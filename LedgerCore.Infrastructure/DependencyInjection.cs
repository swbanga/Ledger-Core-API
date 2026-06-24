using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using LedgerCore.Application.Data;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Persistence.Interceptors;
using LedgerCore.Infrastructure.BackgroundJobs;
using MassTransit;
using MediatR;
using LedgerCore.Application.Behaviors;
using LedgerCore.Infrastructure;
using StackExchange.Redis;
using LedgerCore.Infrastructure.Locking;
using LedgerCore.Infrastructure.Services;
using LedgerCore.Application.Contracts;
using LedgerCore.Infrastructure.Caching;
using LedgerCore.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LedgerCore.Infrastructure.Health;

namespace LedgerCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<LedgerDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("Database"), sqlOptions => sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null))
                   .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        // temporary test
        // services.AddDbContext<LedgerDbContext>((sp, options) =>
        //     options.UseSqlServer(configuration.GetConnectionString("Database"), sqlOptions => sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null))
        //            .EnableSensitiveDataLogging()
        //            .LogTo(Console.WriteLine));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LedgerDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountLockService, AccountLockService>();
        services.AddScoped<IProjectionRebuildService, ProjectionRebuildService>();
        services.AddSingleton<OutboxProcessorService>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxProcessorService>());

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConn = config.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(redisConn))
            {
                throw new InvalidOperationException("FATAL: Redis connection string is missing.");
            }
            return ConnectionMultiplexer.Connect(redisConn);
        });

        services.AddSingleton<ICachingService, RedisCachingService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "LedgerCore_";
        });

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "rabbitmq://localhost");
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .Validate(settings => !string.IsNullOrEmpty(settings.Secret) && settings.Secret.Length >= 32,
                      "JWT secret must be at least 32 characters and non‑empty.")
            .ValidateOnStart();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LedgerCore.Application.Behaviors.LoggingBehavior<,>));

        services.AddHealthChecks()
            .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck<SqlServerHealthCheck>("sqlserver", tags: new[] { "ready", "db" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "cache" })
            .AddCheck<OutboxProcessorHealthCheck>("outbox_processor", tags: new[] { "ready" });

        return services;
    }
}
