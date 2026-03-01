using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceCatalog;

/// <summary>
/// Query to retrieve the full service catalog (categories with items).
/// </summary>
public sealed record GetServiceCatalogQuery : IRequest<IReadOnlyList<ServiceCatalogCategoryDto>>;
