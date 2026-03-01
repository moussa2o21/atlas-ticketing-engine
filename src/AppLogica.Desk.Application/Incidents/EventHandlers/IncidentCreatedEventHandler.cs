using AppLogica.Desk.Domain.Incidents.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Incidents.EventHandlers;

/// <summary>
/// Handles <see cref="IncidentCreatedEvent"/> by logging the creation and
/// preparing for Event Bus publication. Actual MassTransit publish will be
/// wired in Phase 5 — for now, logs the event for observability.
/// </summary>
public sealed class IncidentCreatedEventHandler : INotificationHandler<IncidentCreatedEvent>
{
    private readonly ILogger<IncidentCreatedEventHandler> _logger;

    public IncidentCreatedEventHandler(ILogger<IncidentCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Incident created: {TicketNumber} (Id: {IncidentId}, Tenant: {TenantId}, Priority: {Priority})",
            notification.TicketNumber,
            notification.IncidentId,
            notification.TenantId,
            notification.Priority);

        // TODO Phase 5: Publish desk.incident.created to ATLAS Event Bus via MassTransit
        // await _publishEndpoint.Publish(new IncidentCreatedIntegrationEvent(...), cancellationToken);

        return Task.CompletedTask;
    }
}
