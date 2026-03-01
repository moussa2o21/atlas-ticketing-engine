using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.ApproveServiceRequest;

/// <summary>
/// Handles <see cref="ApproveServiceRequestCommand"/> by transitioning a pending
/// service request to the Approved state.
/// </summary>
public sealed class ApproveServiceRequestCommandHandler : IRequestHandler<ApproveServiceRequestCommand>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ITenantContext _tenantContext;

    public ApproveServiceRequestCommandHandler(
        IServiceRequestRepository requestRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(ApproveServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var serviceRequest = await _requestRepository.GetByIdAsync(
            request.ServiceRequestId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Service request '{request.ServiceRequestId}' not found for tenant '{tenantId}'.");

        serviceRequest.Approve();

        await _requestRepository.UpdateAsync(serviceRequest, cancellationToken);
    }
}
