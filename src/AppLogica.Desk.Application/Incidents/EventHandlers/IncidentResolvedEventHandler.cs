using AppLogica.Desk.Domain.Incidents.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Incidents.EventHandlers;

/// <summary>
/// Handles <see cref="IncidentResolvedEvent"/> by logging the resolution and
/// preparing for Event Bus publication. Actual MassTransit publish will be
/// wired in Phase 5.
/// </summary>
public sealed class IncidentResolvedEventHandler : INotificationHandler<IncidentResolvedEvent>
{
    private readonly ILogger<IncidentResolvedEventHandler> _logger;

    public IncidentResolvedEventHandler(ILogger<IncidentResolvedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(IncidentResolvedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Incident resolved: {IncidentId} (Tenant: {TenantId}, Notes: {ResolutionNotes})",
            notification.IncidentId,
            notification.TenantId,
            notification.ResolutionNotes);

        // TODO Phase 5: Publish desk.incident.resolved to ATLAS Event Bus via MassTransit
        // await _publishEndpoint.Publish(new IncidentResolvedIntegrationEvent(...), cancellationToken);

        return Task.CompletedTask;
    }
}
