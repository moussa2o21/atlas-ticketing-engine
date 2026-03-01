using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Application.Incidents.DTOs;

/// <summary>
/// Summary DTO for incident list views.
/// </summary>
public sealed record IncidentDto(
    Guid Id,
    string TicketNumber,
    string Title,
    IncidentStatus Status,
    Priority Priority,
    Guid? AssigneeId,
    DateTime CreatedAt);
