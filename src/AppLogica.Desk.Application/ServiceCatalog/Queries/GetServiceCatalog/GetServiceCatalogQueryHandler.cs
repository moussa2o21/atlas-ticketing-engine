using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceCatalog;

/// <summary>
/// Handles <see cref="GetServiceCatalogQuery"/> by returning all active categories
/// with their associated catalog items.
/// </summary>
public sealed class GetServiceCatalogQueryHandler
    : IRequestHandler<GetServiceCatalogQuery, IReadOnlyList<ServiceCatalogCategoryDto>>
{
    private readonly IServiceCatalogRepository _catalogRepository;
    private readonly ITenantContext _tenantContext;

    public GetServiceCatalogQueryHandler(
        IServiceCatalogRepository catalogRepository,
        ITenantContext tenantContext)
    {
        _catalogRepository = catalogRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ServiceCatalogCategoryDto>> Handle(
        GetServiceCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var categories = await _catalogRepository.ListCategoriesAsync(tenantId, cancellationToken);

        return categories
            .Select(c => new ServiceCatalogCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ParentCategoryId,
                c.SortOrder,
                c.IsActive,
                c.Items
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ServiceCatalogItemDto(
                        i.Id,
                        i.Name,
                        i.Description,
                        i.CategoryId,
                        i.FulfillmentInstructions,
                        i.ExpectedDeliveryMinutes,
                        i.RequiresApproval,
                        i.ApprovalWorkflowId,
                        i.SortOrder,
                        i.IsActive))
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();
    }
}
