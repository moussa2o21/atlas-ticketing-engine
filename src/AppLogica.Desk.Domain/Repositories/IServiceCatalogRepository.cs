using AppLogica.Desk.Domain.ServiceCatalog;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for service catalog entities (categories, items, workflows).
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface IServiceCatalogRepository
{
    // ─── Categories ───

    /// <summary>
    /// Retrieves a category by its ID within the current tenant.
    /// </summary>
    Task<ServiceCatalogCategory?> GetCategoryByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns all active categories for the current tenant, ordered by SortOrder.
    /// </summary>
    Task<IReadOnlyList<ServiceCatalogCategory>> ListCategoriesAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Persists a new category.
    /// </summary>
    Task AddCategoryAsync(ServiceCatalogCategory category, CancellationToken ct);

    // ─── Items ───

    /// <summary>
    /// Retrieves a catalog item by its ID within the current tenant.
    /// </summary>
    Task<ServiceCatalogItem?> GetItemByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns all active items for a given category, ordered by SortOrder.
    /// </summary>
    Task<IReadOnlyList<ServiceCatalogItem>> ListItemsByCategoryAsync(Guid categoryId, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns all active items for the current tenant, ordered by SortOrder.
    /// </summary>
    Task<IReadOnlyList<ServiceCatalogItem>> ListAllItemsAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Persists a new catalog item.
    /// </summary>
    Task AddItemAsync(ServiceCatalogItem item, CancellationToken ct);

    // ─── Workflows ───

    /// <summary>
    /// Retrieves an approval workflow by its ID within the current tenant.
    /// </summary>
    Task<ApprovalWorkflow?> GetWorkflowByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Persists a new approval workflow.
    /// </summary>
    Task AddWorkflowAsync(ApprovalWorkflow workflow, CancellationToken ct);
}
