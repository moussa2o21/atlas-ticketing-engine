using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Common.IntegrationEvents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Infrastructure.EventBus;
using AppLogica.Desk.Infrastructure.Persistence;
using AppLogica.Desk.Infrastructure.Persistence.Repositories;
using AppLogica.Desk.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddScoped<IBusinessHoursRepository, BusinessHoursRepository>();

        // Services
        services.AddScoped<IBusinessHoursCalculator, BusinessHoursCalculator>();

        // ── MassTransit + RabbitMQ (ATLAS Event Bus) ────────────────────────
        var eventBusConnectionString = configuration["EventBus:ConnectionString"];

        if (!string.IsNullOrEmpty(eventBusConnectionString))
        {
            services.AddMassTransit(bus =>
            {
                bus.AddConsumer<DeskEventBusConsumer>();

                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(eventBusConnectionString));

                    // Receive endpoint: atlas.desk.events bound to atlas.events (topic) with routing key desk.#
                    cfg.ReceiveEndpoint("atlas.desk.events", ep =>
                    {
                        ep.ConfigureConsumeTopology = false;

                        ep.Bind("atlas.events", exchange =>
                        {
                            exchange.ExchangeType = "topic";
                            exchange.RoutingKey = "desk.#";
                        });

                        ep.ConfigureConsumer<DeskEventBusConsumer>(context);
                    });

                    // Message topology: publish IncidentCreatedIntegrationEvent to atlas.events exchange
                    cfg.Message<IncidentCreatedIntegrationEvent>(x =>
                    {
                        x.SetEntityName("atlas.events");
                    });

                    cfg.Publish<IncidentCreatedIntegrationEvent>(x =>
                    {
                        x.ExchangeType = "topic";
                    });

                    cfg.Send<IncidentCreatedIntegrationEvent>(x =>
                    {
                        x.UseRoutingKeyFormatter(_ => "desk.incident.created");
                    });
                });
            });
        }
        else
        {
            // MassTransit not configured — test/local environment without RabbitMQ
            var sp = services.BuildServiceProvider();
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("AppLogica.Desk.Infrastructure");
            logger?.LogWarning(
                "EventBus:ConnectionString is not configured. MassTransit/RabbitMQ will not be registered. " +
                "Set EVENTBUS__CONNECTIONSTRING to enable the ATLAS Event Bus.");
        }

        return services;
    }
}
