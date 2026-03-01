using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Queries.ListIncidents;

/// <summary>
/// Handles <see cref="ListIncidentsQuery"/> by mapping filter criteria to
/// <see cref="IncidentFilter"/> and returning a paginated list of <see cref="IncidentDto"/>.
/// </summary>
public sealed class ListIncidentsQueryHandler
    : IRequestHandler<ListIncidentsQuery, PagedResult<IncidentDto>>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public ListIncidentsQueryHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<IncidentDto>> Handle(
        ListIncidentsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var filter = new IncidentFilter
        {
            TenantId = tenantId,
            Statuses = request.Statuses,
            Priorities = request.Priorities,
            AssigneeId = request.AssigneeId,
            QueueId = request.QueueId,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            SearchQuery = request.SearchQuery,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var incidents = await _incidentRepository.ListAsync(filter, cancellationToken);

        var items = incidents
            .Select(i => new IncidentDto(
                i.Id,
                i.TicketNumber,
                i.Title,
                i.Status,
                i.Priority,
                i.AssigneeId,
                i.CreatedAt))
            .ToList()
            .AsReadOnly();

        // NOTE: The repository's ListAsync returns only the current page of items.
        // A proper TotalCount requires a separate CountAsync method on the repository,
        // which will be added in a future iteration. For now, we estimate based on
        // whether a full page was returned.
        var totalCount = items.Count < request.PageSize && request.Page == 1
            ? items.Count
            : items.Count + ((request.Page - 1) * request.PageSize) + (items.Count == request.PageSize ? 1 : 0);

        return new PagedResult<IncidentDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);
    }
}
