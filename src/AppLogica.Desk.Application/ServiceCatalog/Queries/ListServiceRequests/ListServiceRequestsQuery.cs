using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using AppLogica.Desk.Domain.ServiceCatalog;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.ListServiceRequests;

/// <summary>
/// Query to list service requests with filtering and pagination.
/// </summary>
public sealed record ListServiceRequestsQuery(
    List<ServiceRequestStatus>? Statuses,
    Guid? RequesterId,
    Guid? CatalogItemId,
    Guid? AssigneeId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ServiceRequestDto>>;
