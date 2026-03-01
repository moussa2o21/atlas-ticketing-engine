namespace AppLogica.Desk.Application.Sla.EventHandlers;

/// <summary>
/// Abstraction for pushing SLA notifications to connected clients (e.g. via SignalR).
/// Implemented in the Infrastructure/API layer.
/// </summary>
public interface ISlaNotificationService
{
    Task SendSlaWarningAsync(Guid tenantId, Guid incidentId, Guid timerId, CancellationToken ct = default);
    Task SendSlaBreachedAsync(Guid tenantId, Guid incidentId, Guid timerId, CancellationToken ct = default);
}
