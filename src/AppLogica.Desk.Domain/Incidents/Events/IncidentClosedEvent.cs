using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Incidents.Events;

/// <summary>
/// Raised when an incident is closed after resolution.
/// </summary>
public sealed record IncidentClosedEvent(
    Guid IncidentId,
    Guid TenantId) : IDomainEvent;
