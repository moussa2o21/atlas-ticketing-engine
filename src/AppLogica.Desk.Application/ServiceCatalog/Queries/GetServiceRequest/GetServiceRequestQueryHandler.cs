using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceRequest;

/// <summary>
/// Handles <see cref="GetServiceRequestQuery"/> by returning a full
/// <see cref="ServiceRequestDetailDto"/>.
/// </summary>
public sealed class GetServiceRequestQueryHandler
    : IRequestHandler<GetServiceRequestQuery, ServiceRequestDetailDto>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ITenantContext _tenantContext;

    public GetServiceRequestQueryHandler(
        IServiceRequestRepository requestRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ServiceRequestDetailDto> Handle(
        GetServiceRequestQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var serviceRequest = await _requestRepository.GetByIdAsync(
            request.ServiceRequestId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Service request '{request.ServiceRequestId}' not found for tenant '{tenantId}'.");

        return new ServiceRequestDetailDto(
            serviceRequest.Id,
            serviceRequest.RequestNumber,
            serviceRequest.Title,
            serviceRequest.Description,
            serviceRequest.Status,
            serviceRequest.ApprovalStatus,
            serviceRequest.CatalogItemId,
            serviceRequest.RequesterId,
            serviceRequest.AssigneeId,
            serviceRequest.FulfillmentNotes,
            serviceRequest.FulfilledAt,
            serviceRequest.CancelledAt,
            serviceRequest.CancellationReason,
            serviceRequest.CreatedAt,
            serviceRequest.UpdatedAt);
    }
}
