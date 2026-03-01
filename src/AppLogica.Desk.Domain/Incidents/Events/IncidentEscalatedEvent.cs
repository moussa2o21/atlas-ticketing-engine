using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Incidents.Events;

/// <summary>
/// Raised when an incident is escalated.
/// </summary>
public sealed record IncidentEscalatedEvent(
    Guid IncidentId,
    Guid TenantId,
    string Reason) : IDomainEvent;
