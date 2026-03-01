using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.GetIncidentTimeline;

/// <summary>
/// Handles <see cref="GetIncidentTimelineQuery"/> by building a timeline from the
/// incident's current state. In a full implementation, this would pull entries from
/// an audit log or event store for a complete activity history.
/// </summary>
public sealed class GetIncidentTimelineQueryHandler
    : IRequestHandler<GetIncidentTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public GetIncidentTimelineQueryHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetIncidentTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        // Build timeline from the incident's current state.
        // Phase 2+ will integrate with the audit_log table for full event sourcing.
        var timeline = new List<TimelineEntryDto>
        {
            new(
                incident.CreatedAt,
                "Created",
                incident.CreatedBy,
                $"Incident {incident.TicketNumber} created with priority {incident.Priority}")
        };

        if (incident.AssigneeId.HasValue)
        {
            timeline.Add(new TimelineEntryDto(
                incident.UpdatedAt ?? incident.CreatedAt,
                "Assigned",
                incident.UpdatedBy,
                $"Assigned to agent {incident.AssigneeId.Value}"));
        }

        if (incident.ResolvedAt.HasValue)
        {
            timeline.Add(new TimelineEntryDto(
                incident.ResolvedAt.Value,
                "Resolved",
                incident.UpdatedBy,
                incident.ResolutionNotes));
        }

        if (incident.ClosedAt.HasValue)
        {
            timeline.Add(new TimelineEntryDto(
                incident.ClosedAt.Value,
                "Closed",
                incident.UpdatedBy,
                null));
        }

        return timeline.AsReadOnly();
    }
}
