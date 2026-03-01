using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.ServiceCatalog;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.CreateServiceRequest;

/// <summary>
/// Handles <see cref="CreateServiceRequestCommand"/> by creating a new service request,
/// generating a request number, and submitting it.
/// </summary>
public sealed class CreateServiceRequestCommandHandler : IRequestHandler<CreateServiceRequestCommand, Guid>
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly IServiceCatalogRepository _catalogRepository;
    private readonly ITenantContext _tenantContext;

    public CreateServiceRequestCommandHandler(
        IServiceRequestRepository requestRepository,
        IServiceCatalogRepository catalogRepository,
        ITenantContext tenantContext)
    {
        _requestRepository = requestRepository;
        _catalogRepository = catalogRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(CreateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;

        // Validate catalog item exists
        var catalogItem = await _catalogRepository.GetItemByIdAsync(
            request.CatalogItemId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Catalog item '{request.CatalogItemId}' not found for tenant '{tenantId}'.");

        // Generate request number: SRQ-{year:D4}-{seq:D5}
        var year = now.Year;
        var sequence = await _requestRepository.GetNextRequestSequenceAsync(tenantId, year, cancellationToken);
        var requestNumber = $"SRQ-{year:D4}-{sequence:D5}";

        // Create the service request via its factory method
        var serviceRequest = ServiceRequest.Create(
            tenantId,
            requestNumber,
            request.Title,
            request.Description,
            request.CatalogItemId,
            requesterId: Guid.Empty, // TODO: resolve from ICurrentUserContext
            requiresApproval: catalogItem.RequiresApproval);

        // Immediately submit the request
        serviceRequest.Submit();

        await _requestRepository.AddAsync(serviceRequest, cancellationToken);

        return serviceRequest.Id;
    }
}
