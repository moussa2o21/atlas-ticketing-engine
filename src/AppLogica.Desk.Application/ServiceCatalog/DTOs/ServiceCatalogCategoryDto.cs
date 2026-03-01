namespace AppLogica.Desk.Application.ServiceCatalog.DTOs;

/// <summary>
/// DTO for a service catalog category with its items.
/// </summary>
public sealed record ServiceCatalogCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<ServiceCatalogItemDto> Items);
