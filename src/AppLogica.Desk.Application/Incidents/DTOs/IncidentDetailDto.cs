using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Application.Incidents.DTOs;

/// <summary>
/// Full detail DTO for a single incident, including all fields and timeline entries.
/// </summary>
public sealed record IncidentDetailDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string Description,
    IncidentStatus Status,
    Priority Priority,
    Impact Impact,
    Urgency Urgency,
    IncidentType IncidentType,
    Guid? AssigneeId,
    Guid? QueueId,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    bool IsMajorIncident,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<TimelineEntryDto> Timeline);
