using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IServiceRequestRepository"/>.
/// All queries are automatically scoped to the current tenant via the global query filter
/// on <see cref="DeskDbContext"/>.
/// </summary>
public sealed class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly DeskDbContext _dbContext;

    public ServiceRequestRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ServiceRequest?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceRequest>> ListAsync(ServiceRequestFilter filter, CancellationToken ct)
    {
        var query = _dbContext.ServiceRequests.AsQueryable();

        // TenantId filter (defense-in-depth, global filter also applies)
        query = query.Where(r => r.TenantId == filter.TenantId);

        // Status filter
        if (filter.Statuses is { Count: > 0 })
        {
            query = query.Where(r => filter.Statuses.Contains(r.Status));
        }

        // Requester filter
        if (filter.RequesterId.HasValue)
        {
            query = query.Where(r => r.RequesterId == filter.RequesterId.Value);
        }

        // Catalog item filter
        if (filter.CatalogItemId.HasValue)
        {
            query = query.Where(r => r.CatalogItemId == filter.CatalogItemId.Value);
        }

        // Assignee filter
        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(r => r.AssigneeId == filter.AssigneeId.Value);
        }

        // Default ordering: newest first
        query = query.OrderByDescending(r => r.CreatedAt);

        // Pagination
        var skip = (filter.Page - 1) * filter.PageSize;
        query = query.Skip(skip).Take(filter.PageSize);

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(ServiceRequest request, CancellationToken ct)
    {
        await _dbContext.ServiceRequests.AddAsync(request, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ServiceRequest request, CancellationToken ct)
    {
        _dbContext.ServiceRequests.Update(request);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> GetNextRequestSequenceAsync(Guid tenantId, int year, CancellationToken ct)
    {
        var prefix = $"SRQ-{year}-";

        var maxSequence = await _dbContext.ServiceRequests
            .Where(r => r.TenantId == tenantId && r.RequestNumber.StartsWith(prefix))
            .Select(r => r.RequestNumber)
            .OrderByDescending(rn => rn)
            .FirstOrDefaultAsync(ct);

        if (maxSequence is null)
        {
            return 1;
        }

        var sequencePart = maxSequence[prefix.Length..];
        if (int.TryParse(sequencePart, out var currentMax))
        {
            return currentMax + 1;
        }

        return 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceRequest>> GetTimedOutApprovalsAsync(Guid tenantId, CancellationToken ct)
    {
        // Find service requests in PendingApproval status where the approval has timed out.
        // We join with the catalog item and approval workflow to get the timeout value.
        var now = DateTime.UtcNow;

        var timedOutRequests = await (
            from sr in _dbContext.ServiceRequests
            join ci in _dbContext.ServiceCatalogItems on sr.CatalogItemId equals ci.Id
            join aw in _dbContext.ApprovalWorkflows on ci.ApprovalWorkflowId equals aw.Id
            where sr.TenantId == tenantId
                  && sr.Status == ServiceRequestStatus.PendingApproval
                  && sr.CreatedAt.AddMinutes(aw.TimeoutMinutes) < now
            select sr
        ).ToListAsync(ct);

        return timedOutRequests;
    }
}
