using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Incidents.Events;

/// <summary>
/// Raised when an incident is resolved.
/// </summary>
public sealed record IncidentResolvedEvent(
    Guid IncidentId,
    Guid TenantId,
    string ResolutionNotes) : IDomainEvent;
