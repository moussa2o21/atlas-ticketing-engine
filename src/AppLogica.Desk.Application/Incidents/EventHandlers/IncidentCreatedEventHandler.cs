using AppLogica.Desk.Application.Common.IntegrationEvents;
using AppLogica.Desk.Domain.Incidents.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Incidents.EventHandlers;

/// <summary>
/// Handles <see cref="IncidentCreatedEvent"/> by logging the creation and
/// publishing desk.incident.created to the ATLAS Event Bus via MassTransit.
/// If MassTransit is not configured (e.g. test environments), the publish
/// step is skipped gracefully.
/// </summary>
public sealed class IncidentCreatedEventHandler : INotificationHandler<IncidentCreatedEvent>
{
    private readonly ILogger<IncidentCreatedEventHandler> _logger;
    private readonly IPublishEndpoint? _publishEndpoint;

    public IncidentCreatedEventHandler(
        ILogger<IncidentCreatedEventHandler> logger,
        IPublishEndpoint? publishEndpoint = null)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Incident created: {TicketNumber} (Id: {IncidentId}, Tenant: {TenantId}, Priority: {Priority})",
            notification.TicketNumber,
            notification.IncidentId,
            notification.TenantId,
            notification.Priority);

        if (_publishEndpoint is null)
        {
            _logger.LogWarning(
                "IPublishEndpoint is not available — skipping EventBus publish for desk.incident.created ({TicketNumber})",
                notification.TicketNumber);
            return;
        }

        await _publishEndpoint.Publish(
            new IncidentCreatedIntegrationEvent(
                notification.IncidentId,
                notification.TenantId,
                notification.TicketNumber,
                notification.Priority.ToString()),
            cancellationToken);

        _logger.LogInformation(
            "Published desk.incident.created to EventBus for {TicketNumber}",
            notification.TicketNumber);
    }
}
