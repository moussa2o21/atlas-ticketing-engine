using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISlaRepository"/>.
/// All queries are automatically scoped to the current tenant via the global query filter
/// on <see cref="DeskDbContext"/>.
/// </summary>
public sealed class SlaRepository : ISlaRepository
{
    private readonly DeskDbContext _dbContext;

    public SlaRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<SlaPolicy?> GetPolicyByPriorityAsync(
        Priority priority,
        Guid tenantId,
        CancellationToken ct)
    {
        return await _dbContext.SlaPolicies
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                     && p.Targets.Any(t => t.Priority == priority),
                ct);
    }

    /// <inheritdoc />
    public async Task<SlaTimer?> GetTimerByIncidentIdAsync(
        Guid incidentId,
        Guid tenantId,
        CancellationToken ct)
    {
        return await _dbContext.SlaTimers
            .FirstOrDefaultAsync(
                t => t.IncidentId == incidentId && t.TenantId == tenantId,
                ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SlaTimer>> GetActiveTimersAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        return await _dbContext.SlaTimers
            .Where(t => t.TenantId == tenantId
                        && (t.Status == SlaTimerStatus.Active || t.Status == SlaTimerStatus.Warning))
            .OrderBy(t => t.ResolutionDueAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddTimerAsync(SlaTimer timer, CancellationToken ct)
    {
        await _dbContext.SlaTimers.AddAsync(timer, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateTimerAsync(SlaTimer timer, CancellationToken ct)
    {
        _dbContext.SlaTimers.Update(timer);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddPolicyAsync(SlaPolicy policy, CancellationToken ct)
    {
        await _dbContext.SlaPolicies.AddAsync(policy, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
