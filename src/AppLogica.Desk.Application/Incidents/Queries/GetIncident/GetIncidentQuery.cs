using AppLogica.Desk.Application.Incidents.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.GetIncident;

/// <summary>
/// Query to retrieve the full detail of a single incident.
/// </summary>
public sealed record GetIncidentQuery(
    Guid IncidentId) : IRequest<IncidentDetailDto>;
