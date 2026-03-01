using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IIncidentRepository"/>.
/// All queries are automatically scoped to the current tenant via the global query filter
/// on <see cref="DeskDbContext"/>.
/// </summary>
public sealed class IncidentRepository : IIncidentRepository
{
    private readonly DeskDbContext _dbContext;

    public IncidentRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Incident?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        // The global query filter already enforces TenantId and soft-delete,
        // but we explicitly filter by tenantId for defense-in-depth.
        return await _dbContext.Incidents
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Incident>> ListAsync(IncidentFilter filter, CancellationToken ct)
    {
        var query = _dbContext.Incidents.AsQueryable();

        // TenantId filter (defense-in-depth, global filter also applies)
        query = query.Where(i => i.TenantId == filter.TenantId);

        // Status filter
        if (filter.Statuses is { Count: > 0 })
        {
            query = query.Where(i => filter.Statuses.Contains(i.Status));
        }

        // Priority filter
        if (filter.Priorities is { Count: > 0 })
        {
            query = query.Where(i => filter.Priorities.Contains(i.Priority));
        }

        // Assignee filter
        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(i => i.AssigneeId == filter.AssigneeId.Value);
        }

        // Queue filter
        if (filter.QueueId.HasValue)
        {
            query = query.Where(i => i.QueueId == filter.QueueId.Value);
        }

        // Date range filters
        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= filter.CreatedTo.Value);
        }

        // Free-text search across ticket number, title, and description
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchTerm = filter.SearchQuery.Trim().ToLower();
            query = query.Where(i =>
                EF.Functions.ILike(i.TicketNumber, $"%{searchTerm}%") ||
                EF.Functions.ILike(i.Title, $"%{searchTerm}%") ||
                EF.Functions.ILike(i.Description, $"%{searchTerm}%"));
        }

        // Default ordering: newest first
        query = query.OrderByDescending(i => i.CreatedAt);

        // Pagination
        var skip = (filter.Page - 1) * filter.PageSize;
        query = query.Skip(skip).Take(filter.PageSize);

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(Incident incident, CancellationToken ct)
    {
        await _dbContext.Incidents.AddAsync(incident, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Incident incident, CancellationToken ct)
    {
        _dbContext.Incidents.Update(incident);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> GetNextTicketSequenceAsync(Guid tenantId, int year, CancellationToken ct)
    {
        // Build the prefix pattern for the given year: "INC-YYYY-"
        var prefix = $"INC-{year}-";

        // Find the maximum ticket number for this tenant and year
        var maxSequence = await _dbContext.Incidents
            .Where(i => i.TenantId == tenantId && i.TicketNumber.StartsWith(prefix))
            .Select(i => i.TicketNumber)
            .OrderByDescending(tn => tn)
            .FirstOrDefaultAsync(ct);

        if (maxSequence is null)
        {
            return 1;
        }

        // Extract the numeric suffix after "INC-YYYY-"
        var sequencePart = maxSequence[prefix.Length..];
        if (int.TryParse(sequencePart, out var currentMax))
        {
            return currentMax + 1;
        }

        return 1;
    }
}
