using AppLogica.Desk.Domain.Sla.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Sla.EventHandlers;

/// <summary>
/// Handles SLA breached events by notifying connected agents via SignalR
/// and publishing to the ATLAS Event Bus.
/// </summary>
public sealed class SlaBreachedEventHandler : INotificationHandler<SlaBreachedEvent>
{
    private readonly ILogger<SlaBreachedEventHandler> _logger;
    private readonly ISlaNotificationService? _notificationService;

    public SlaBreachedEventHandler(
        ILogger<SlaBreachedEventHandler> logger,
        ISlaNotificationService? notificationService = null)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SlaBreachedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogError(
            "SLA BREACHED: Timer {TimerId} for incident {IncidentId} (Tenant: {TenantId})",
            notification.TimerId, notification.IncidentId, notification.TenantId);

        if (_notificationService is not null)
        {
            await _notificationService.SendSlaBreachedAsync(
                notification.TenantId,
                notification.IncidentId,
                notification.TimerId,
                cancellationToken);
        }
    }
}
