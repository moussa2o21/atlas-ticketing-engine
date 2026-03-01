using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.FulfillServiceRequest;

/// <summary>
/// Handles <see cref="FulfillServiceRequestCommand"/> by transitioning an
/// in-progress service request to the Fulfilled state.
/// </summary>
public sealed class FulfillServiceRequestCommandHandler : IRequestHandler<FulfillServiceRequestCommand>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ITenantContext _tenantContext;

    public FulfillServiceRequestCommandHandler(
        IServiceRequestRepository requestRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(FulfillServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var serviceRequest = await _requestRepository.GetByIdAsync(
            request.ServiceRequestId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Service request '{request.ServiceRequestId}' not found for tenant '{tenantId}'.");

        serviceRequest.Fulfill(request.FulfillmentNotes);

        await _requestRepository.UpdateAsync(serviceRequest, cancellationToken);
    }
}
