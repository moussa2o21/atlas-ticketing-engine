using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.RejectServiceRequest;

/// <summary>
/// Handles <see cref="RejectServiceRequestCommand"/> by transitioning a pending
/// service request to the Rejected state.
/// </summary>
public sealed class RejectServiceRequestCommandHandler : IRequestHandler<RejectServiceRequestCommand>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ITenantContext _tenantContext;

    public RejectServiceRequestCommandHandler(
        IServiceRequestRepository requestRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(RejectServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var serviceRequest = await _requestRepository.GetByIdAsync(
            request.ServiceRequestId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Service request '{request.ServiceRequestId}' not found for tenant '{tenantId}'.");

        serviceRequest.Reject(request.Reason);

        await _requestRepository.UpdateAsync(serviceRequest, cancellationToken);
    }
}
