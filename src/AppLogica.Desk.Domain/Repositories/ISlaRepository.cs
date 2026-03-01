using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Sla;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for SLA policy and timer persistence.
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface ISlaRepository
{
    /// <summary>
    /// Retrieves the SLA policy matching the given priority for a tenant.
    /// </summary>
    Task<SlaPolicy?> GetPolicyByPriorityAsync(Priority priority, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Retrieves the SLA timer for a specific incident within a tenant.
    /// </summary>
    Task<SlaTimer?> GetTimerByIncidentIdAsync(Guid incidentId, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns all active (non-terminated) SLA timers for a tenant.
    /// Used by the Hangfire SLA evaluation job.
    /// </summary>
    Task<IReadOnlyList<SlaTimer>> GetActiveTimersAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Persists a new SLA timer.
    /// </summary>
    Task AddTimerAsync(SlaTimer timer, CancellationToken ct);

    /// <summary>
    /// Updates an existing SLA timer.
    /// </summary>
    Task UpdateTimerAsync(SlaTimer timer, CancellationToken ct);

    /// <summary>
    /// Persists a new SLA policy.
    /// </summary>
    Task AddPolicyAsync(SlaPolicy policy, CancellationToken ct);
}
