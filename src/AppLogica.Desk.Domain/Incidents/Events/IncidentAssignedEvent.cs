using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Incidents.Events;

/// <summary>
/// Raised when an incident is assigned to an agent.
/// </summary>
public sealed record IncidentAssignedEvent(
    Guid IncidentId,
    Guid TenantId,
    Guid AssigneeId) : IDomainEvent;
