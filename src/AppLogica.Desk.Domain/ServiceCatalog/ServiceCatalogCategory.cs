using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Represents a category in the service catalog. Supports hierarchical nesting
/// via <see cref="ParentCategoryId"/>.
/// </summary>
public sealed class ServiceCatalogCategory : Entity
{
    private readonly List<ServiceCatalogItem> _items = [];

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyList<ServiceCatalogItem> Items => _items.AsReadOnly();

    // EF Core requires a parameterless constructor
    private ServiceCatalogCategory() { }

    /// <summary>
    /// Creates a new service catalog category.
    /// </summary>
    public static ServiceCatalogCategory Create(
        Guid tenantId,
        string name,
        string? description,
        Guid? parentCategoryId,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new ServiceCatalogCategory
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            ParentCategoryId = parentCategoryId,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
