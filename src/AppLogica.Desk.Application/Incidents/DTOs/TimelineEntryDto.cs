namespace AppLogica.Desk.Application.Incidents.DTOs;

/// <summary>
/// Represents a single timeline entry for an incident's activity history.
/// </summary>
public sealed record TimelineEntryDto(
    DateTime Timestamp,
    string Action,
    Guid? ActorId,
    string? Details);
