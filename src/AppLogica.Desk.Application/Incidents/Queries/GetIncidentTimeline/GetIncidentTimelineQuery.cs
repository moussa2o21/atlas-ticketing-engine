using AppLogica.Desk.Application.Incidents.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.GetIncidentTimeline;

/// <summary>
/// Query to retrieve the activity timeline for a specific incident.
/// </summary>
public sealed record GetIncidentTimelineQuery(
    Guid IncidentId) : IRequest<IReadOnlyList<TimelineEntryDto>>;
