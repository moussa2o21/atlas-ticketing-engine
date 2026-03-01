using AppLogica.Desk.Domain.Sla.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Sla.EventHandlers;

/// <summary>
/// Handles SLA warning events by notifying connected agents via SignalR
/// and publishing to the ATLAS Event Bus.
/// </summary>
public sealed class SlaWarningEventHandler : INotificationHandler<SlaWarningEvent>
{
    private readonly ILogger<SlaWarningEventHandler> _logger;
    private readonly ISlaNotificationService? _notificationService;

    public SlaWarningEventHandler(
        ILogger<SlaWarningEventHandler> logger,
        ISlaNotificationService? notificationService = null)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SlaWarningEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "SLA WARNING raised: Timer {TimerId} for incident {IncidentId} (Tenant: {TenantId})",
            notification.TimerId, notification.IncidentId, notification.TenantId);

        if (_notificationService is not null)
        {
            await _notificationService.SendSlaWarningAsync(
                notification.TenantId,
                notification.IncidentId,
                notification.TimerId,
                cancellationToken);
        }
    }
}
