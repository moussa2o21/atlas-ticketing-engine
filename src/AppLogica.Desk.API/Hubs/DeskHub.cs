using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AppLogica.Desk.API.Hubs;

/// <summary>
/// SignalR hub for real-time agent inbox updates. Clients join a group based on their
/// TenantId from JWT claims so that incidents, SLA warnings, and SLA breaches are
/// broadcast only to agents within the same tenant.
/// </summary>
[Authorize]
public sealed class DeskHub : Hub
{
    private const string TenantClaimType = "tenant_id";

    /// <summary>
    /// When a client connects, automatically join them to a group named after their TenantId.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var tenantId = GetTenantId();
        if (tenantId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// When a client disconnects, they are automatically removed from all groups by SignalR.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends an incident-updated notification to all connected clients in the specified tenant group.
    /// </summary>
    public async Task SendIncidentUpdated(string tenantId, Guid incidentId, string ticketNumber)
    {
        await Clients.Group(tenantId).SendAsync("IncidentUpdated", new
        {
            IncidentId = incidentId,
            TicketNumber = ticketNumber,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends an SLA warning notification to all connected clients in the specified tenant group.
    /// </summary>
    public async Task SendSlaWarning(string tenantId, Guid incidentId, object timerInfo)
    {
        await Clients.Group(tenantId).SendAsync("SlaWarning", new
        {
            IncidentId = incidentId,
            TimerInfo = timerInfo,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends an SLA breached notification to all connected clients in the specified tenant group.
    /// </summary>
    public async Task SendSlaBreached(string tenantId, Guid incidentId, object timerInfo)
    {
        await Clients.Group(tenantId).SendAsync("SlaBreached", new
        {
            IncidentId = incidentId,
            TimerInfo = timerInfo,
            Timestamp = DateTime.UtcNow
        });
    }

    private string? GetTenantId()
    {
        return Context.User?.FindFirst(TenantClaimType)?.Value;
    }
}
