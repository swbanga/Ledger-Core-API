using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using LedgerCore.Application.Data;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Data.Interceptors;
using LedgerCore.Infrastructure.BackgroundJobs;

namespace LedgerCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<LedgerDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                   .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LedgerDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<OutboxProcessorJob>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379,password=DevSecOps#2026_Cache";
            options.InstanceName = "Ledger_";
        });

        return services;
    }
}
