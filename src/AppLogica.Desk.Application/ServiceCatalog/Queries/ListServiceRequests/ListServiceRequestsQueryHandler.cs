using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.ListServiceRequests;

/// <summary>
/// Handles <see cref="ListServiceRequestsQuery"/> by mapping filter criteria to
/// <see cref="ServiceRequestFilter"/> and returning a paginated list of
/// <see cref="ServiceRequestDto"/>.
/// </summary>
public sealed class ListServiceRequestsQueryHandler
    : IRequestHandler<ListServiceRequestsQuery, PagedResult<ServiceRequestDto>>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ITenantContext _tenantContext;

    public ListServiceRequestsQueryHandler(
        IServiceRequestRepository requestRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<ServiceRequestDto>> Handle(
        ListServiceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var filter = new ServiceRequestFilter
        {
            TenantId = tenantId,
            Statuses = request.Statuses,
            RequesterId = request.RequesterId,
            CatalogItemId = request.CatalogItemId,
            AssigneeId = request.AssigneeId,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var serviceRequests = await _requestRepository.ListAsync(filter, cancellationToken);

        var items = serviceRequests
            .Select(r => new ServiceRequestDto(
                r.Id,
                r.RequestNumber,
                r.Title,
                r.Status,
                r.ApprovalStatus,
                r.CatalogItemId,
                r.RequesterId,
                r.AssigneeId,
                r.CreatedAt))
            .ToList()
            .AsReadOnly();

        // NOTE: Same estimation approach as ListIncidentsQueryHandler.
        // A proper TotalCount requires a separate CountAsync method.
        var totalCount = items.Count < request.PageSize && request.Page == 1
            ? items.Count
            : items.Count + ((request.Page - 1) * request.PageSize) + (items.Count == request.PageSize ? 1 : 0);

        return new PagedResult<ServiceRequestDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);
    }
}
