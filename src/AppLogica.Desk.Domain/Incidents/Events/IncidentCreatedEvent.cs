using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Incidents.Events;

/// <summary>
/// Raised when a new incident is created.
/// </summary>
public sealed record IncidentCreatedEvent(
    Guid IncidentId,
    Guid TenantId,
    string TicketNumber,
    Priority Priority,
    Impact Impact,
    Urgency Urgency) : IDomainEvent;
