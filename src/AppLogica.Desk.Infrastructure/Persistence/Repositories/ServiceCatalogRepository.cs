using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IServiceCatalogRepository"/>.
/// All queries are automatically scoped to the current tenant via the global query filter
/// on <see cref="DeskDbContext"/>.
/// </summary>
public sealed class ServiceCatalogRepository : IServiceCatalogRepository
{
    private readonly DeskDbContext _dbContext;

    public ServiceCatalogRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ─── Categories ───

    /// <inheritdoc />
    public async Task<ServiceCatalogCategory?> GetCategoryByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceCatalogCategories
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceCatalogCategory>> ListCategoriesAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceCatalogCategories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .Include(c => c.Items)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddCategoryAsync(ServiceCatalogCategory category, CancellationToken ct)
    {
        await _dbContext.ServiceCatalogCategories.AddAsync(category, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    // ─── Items ───

    /// <inheritdoc />
    public async Task<ServiceCatalogItem?> GetItemByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceCatalogItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceCatalogItem>> ListItemsByCategoryAsync(
        Guid categoryId, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceCatalogItems
            .Where(i => i.TenantId == tenantId && i.CategoryId == categoryId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceCatalogItem>> ListAllItemsAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceCatalogItems
            .Where(i => i.TenantId == tenantId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddItemAsync(ServiceCatalogItem item, CancellationToken ct)
    {
        await _dbContext.ServiceCatalogItems.AddAsync(item, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    // ─── Workflows ───

    /// <inheritdoc />
    public async Task<ApprovalWorkflow?> GetWorkflowByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task AddWorkflowAsync(ApprovalWorkflow workflow, CancellationToken ct)
    {
        await _dbContext.ApprovalWorkflows.AddAsync(workflow, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
