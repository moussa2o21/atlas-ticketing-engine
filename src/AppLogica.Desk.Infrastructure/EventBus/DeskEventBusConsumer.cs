using MassTransit;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Infrastructure.EventBus;

public sealed class DeskEventBusConsumer : IConsumer<IncidentCreatedIntegrationEvent>
{
    private readonly ILogger<DeskEventBusConsumer> _logger;

    public DeskEventBusConsumer(ILogger<DeskEventBusConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<IncidentCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "EventBus consumed: desk.incident.created — {TicketNumber} (Tenant: {TenantId})",
            context.Message.TicketNumber, context.Message.TenantId);
        return Task.CompletedTask;
    }
}
