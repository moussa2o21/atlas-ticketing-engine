using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.GetIncident;

/// <summary>
/// Handles <see cref="GetIncidentQuery"/> by returning a full <see cref="IncidentDetailDto"/>.
/// </summary>
public sealed class GetIncidentQueryHandler : IRequestHandler<GetIncidentQuery, IncidentDetailDto>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public GetIncidentQueryHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IncidentDetailDto> Handle(
        GetIncidentQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        // Build timeline from the incident's current state.
        // In a full implementation, this would pull from an audit log or event store.
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

        return new IncidentDetailDto(
            incident.Id,
            incident.TicketNumber,
            incident.Title,
            incident.Description,
            incident.Status,
            incident.Priority,
            incident.Impact,
            incident.Urgency,
            incident.IncidentType,
            incident.AssigneeId,
            incident.QueueId,
            incident.ResolutionNotes,
            incident.ResolvedAt,
            incident.ClosedAt,
            incident.IsMajorIncident,
            incident.CreatedAt,
            incident.UpdatedAt,
            timeline.AsReadOnly());
    }
}
