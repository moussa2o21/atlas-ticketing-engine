using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Sla.Events;

/// <summary>
/// Raised when an SLA timer has breached its deadline.
/// </summary>
public sealed record SlaBreachedEvent(
    Guid TimerId,
    Guid IncidentId,
    Guid TenantId) : IDomainEvent;
