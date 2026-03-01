using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Infrastructure.Persistence;
using AppLogica.Desk.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogica.Desk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenant context — resolved per-request from JWT claims
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        // EF Core DbContext with Npgsql
        var connectionString = configuration["DB:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Database connection string not found. Set 'DB:ConnectionString' in configuration.");

        services.AddDbContext<DeskDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "desk");
            });
        });

        // Repositories
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<ISlaRepository, SlaRepository>();

        return services;
    }
}
