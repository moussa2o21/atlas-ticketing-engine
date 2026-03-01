namespace AppLogica.Desk.Application.Common.IntegrationEvents;

// MassTransit message contracts published to the atlas.events exchange with routing key desk.incident.*
// These live in Application so both Application handlers and Infrastructure consumers can reference them.
public record IncidentCreatedIntegrationEvent(Guid IncidentId, Guid TenantId, string TicketNumber, string Priority);
public record IncidentAssignedIntegrationEvent(Guid IncidentId, Guid TenantId, Guid AssigneeId);
public record IncidentEscalatedIntegrationEvent(Guid IncidentId, Guid TenantId, string Reason);
public record IncidentResolvedIntegrationEvent(Guid IncidentId, Guid TenantId, string ResolutionNotes);
public record IncidentClosedIntegrationEvent(Guid IncidentId, Guid TenantId);
public record SlaWarningIntegrationEvent(Guid TimerId, Guid IncidentId, Guid TenantId);
public record SlaBreachedIntegrationEvent(Guid TimerId, Guid IncidentId, Guid TenantId);
