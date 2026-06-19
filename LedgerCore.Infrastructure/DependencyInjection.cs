using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LedgerCore.Application.Data;

namespace LedgerCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LedgerDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Database")));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LedgerDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
