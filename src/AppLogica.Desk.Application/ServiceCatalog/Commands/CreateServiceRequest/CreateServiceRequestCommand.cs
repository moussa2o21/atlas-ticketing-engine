using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.CreateServiceRequest;

/// <summary>
/// Command to create and submit a new service request against a catalog item.
/// TenantId is resolved from <see cref="Common.Interfaces.ITenantContext"/>, never from the command payload.
/// </summary>
public sealed record CreateServiceRequestCommand(
    string Title,
    string? Description,
    Guid CatalogItemId) : IRequest<Guid>;
