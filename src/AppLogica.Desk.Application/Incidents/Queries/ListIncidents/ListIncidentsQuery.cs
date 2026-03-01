using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Domain.Incidents;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.ListIncidents;

/// <summary>
/// Query to list incidents with filtering and pagination.
/// </summary>
public sealed record ListIncidentsQuery(
    List<IncidentStatus>? Statuses,
    List<Priority>? Priorities,
    Guid? AssigneeId,
    Guid? QueueId,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    string? SearchQuery,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<IncidentDto>>;
