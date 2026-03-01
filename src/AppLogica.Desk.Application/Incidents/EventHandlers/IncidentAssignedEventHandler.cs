using AppLogica.Desk.Domain.Incidents.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Incidents.EventHandlers;

/// <summary>
/// Handles <see cref="IncidentAssignedEvent"/> by logging the assignment and
/// preparing for Event Bus publication. Actual MassTransit publish will be
/// wired in Phase 5.
/// </summary>
public sealed class IncidentAssignedEventHandler : INotificationHandler<IncidentAssignedEvent>
{
    private readonly ILogger<IncidentAssignedEventHandler> _logger;

    public IncidentAssignedEventHandler(ILogger<IncidentAssignedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(IncidentAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Incident assigned: {IncidentId} -> Agent {AssigneeId} (Tenant: {TenantId})",
            notification.IncidentId,
            notification.AssigneeId,
            notification.TenantId);

        // TODO Phase 5: Publish desk.incident.assigned to ATLAS Event Bus via MassTransit
        // await _publishEndpoint.Publish(new IncidentAssignedIntegrationEvent(...), cancellationToken);

        return Task.CompletedTask;
    }
}
