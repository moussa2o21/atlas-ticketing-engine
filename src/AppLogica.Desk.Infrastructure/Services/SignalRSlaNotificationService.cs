using AppLogica.Desk.Application.Sla.EventHandlers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Infrastructure.Services;

/// <summary>
/// Pushes SLA warning and breach notifications to connected agents via the DeskHub SignalR hub.
/// </summary>
public sealed class SignalRSlaNotificationService : ISlaNotificationService
{
    private readonly IHubContext<DeskHubMarker> _hubContext;
    private readonly ILogger<SignalRSlaNotificationService> _logger;

    public SignalRSlaNotificationService(
        IHubContext<DeskHubMarker> hubContext,
        ILogger<SignalRSlaNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendSlaWarningAsync(Guid tenantId, Guid incidentId, Guid timerId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(tenantId.ToString()).SendAsync("SlaWarning", new
        {
            IncidentId = incidentId,
            TimerId = timerId,
            Timestamp = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("SLA warning pushed via SignalR for incident {IncidentId} (Tenant: {TenantId})",
            incidentId, tenantId);
    }

    public async Task SendSlaBreachedAsync(Guid tenantId, Guid incidentId, Guid timerId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(tenantId.ToString()).SendAsync("SlaBreached", new
        {
            IncidentId = incidentId,
            TimerId = timerId,
            Timestamp = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("SLA breach pushed via SignalR for incident {IncidentId} (Tenant: {TenantId})",
            incidentId, tenantId);
    }
}

/// <summary>
/// Marker hub type used for IHubContext DI resolution.
/// The actual DeskHub lives in the API project, but Infrastructure needs
/// a hub type to resolve IHubContext. This marker is mapped to DeskHub in Program.cs.
/// </summary>
public class DeskHubMarker : Hub { }
