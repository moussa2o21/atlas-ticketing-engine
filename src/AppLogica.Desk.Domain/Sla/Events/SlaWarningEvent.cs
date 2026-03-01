using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Sla.Events;

/// <summary>
/// Raised when an SLA timer enters the warning threshold (breach imminent).
/// </summary>
public sealed record SlaWarningEvent(
    Guid TimerId,
    Guid IncidentId,
    Guid TenantId) : IDomainEvent;
