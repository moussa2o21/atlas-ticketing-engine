using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceRequest;

/// <summary>
/// Query to retrieve the full detail of a single service request.
/// </summary>
public sealed record GetServiceRequestQuery(
    Guid ServiceRequestId) : IRequest<ServiceRequestDetailDto>;
