using AppLogica.Desk.Domain.Problems;
using AppLogica.Desk.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProblemRepository"/>.
/// All queries are automatically scoped to the current tenant via the global query filter
/// on <see cref="DeskDbContext"/>.
/// </summary>
public sealed class ProblemRepository : IProblemRepository
{
    private readonly DeskDbContext _dbContext;

    public ProblemRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Problem?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        // The global query filter already enforces TenantId and soft-delete,
        // but we explicitly filter by tenantId for defense-in-depth.
        return await _dbContext.Problems
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Problem>> ListAsync(ProblemFilter filter, CancellationToken ct)
    {
        var query = _dbContext.Problems.AsQueryable();

        // TenantId filter (defense-in-depth, global filter also applies)
        query = query.Where(p => p.TenantId == filter.TenantId);

        // Status filter
        if (filter.Statuses is { Count: > 0 })
        {
            query = query.Where(p => filter.Statuses.Contains(p.Status));
        }

        // Priority filter
        if (filter.Priority.HasValue)
        {
            query = query.Where(p => p.Priority == filter.Priority.Value);
        }

        // Assignee filter
        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(p => p.AssigneeId == filter.AssigneeId.Value);
        }

        // Known error filter
        if (filter.IsKnownError.HasValue)
        {
            query = query.Where(p => p.IsKnownError == filter.IsKnownError.Value);
        }

        // Free-text search across problem number, title, and description
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchTerm = filter.SearchQuery.Trim().ToLower();
            query = query.Where(p =>
                EF.Functions.ILike(p.ProblemNumber, $"%{searchTerm}%") ||
                EF.Functions.ILike(p.Title, $"%{searchTerm}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{searchTerm}%")));
        }

        // Default ordering: newest first
        query = query.OrderByDescending(p => p.CreatedAt);

        // Pagination
        var skip = (filter.Page - 1) * filter.PageSize;
        query = query.Skip(skip).Take(filter.PageSize);

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(Problem problem, CancellationToken ct)
    {
        await _dbContext.Problems.AddAsync(problem, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Problem problem, CancellationToken ct)
    {
        _dbContext.Problems.Update(problem);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> GetNextProblemSequenceAsync(Guid tenantId, int year, CancellationToken ct)
    {
        // Build the prefix pattern for the given year: "PRB-YYYY-"
        var prefix = $"PRB-{year}-";

        // Find the maximum problem number for this tenant and year
        var maxSequence = await _dbContext.Problems
            .Where(p => p.TenantId == tenantId && p.ProblemNumber.StartsWith(prefix))
            .Select(p => p.ProblemNumber)
            .OrderByDescending(pn => pn)
            .FirstOrDefaultAsync(ct);

        if (maxSequence is null)
        {
            return 1;
        }

        // Extract the numeric suffix after "PRB-YYYY-"
        var sequencePart = maxSequence[prefix.Length..];
        if (int.TryParse(sequencePart, out var currentMax))
        {
            return currentMax + 1;
        }

        return 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Problem>> GetKnownErrorsAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.Problems
            .Where(p => p.TenantId == tenantId && p.IsKnownError)
            .OrderByDescending(p => p.KnownErrorPublishedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Problem>> GetByLinkedIncidentIdAsync(Guid incidentId, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.Problems
            .Where(p => p.TenantId == tenantId && p.LinkedIncidentIds.Contains(incidentId))
            .ToListAsync(ct);
    }
}
