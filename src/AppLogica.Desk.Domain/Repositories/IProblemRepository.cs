using AppLogica.Desk.Domain.Problems;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="Problem"/> aggregate persistence.
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface IProblemRepository
{
    /// <summary>
    /// Retrieves a problem by its ID within the specified tenant.
    /// </summary>
    Task<Problem?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns a filtered, paginated list of problems.
    /// </summary>
    Task<IReadOnlyList<Problem>> ListAsync(ProblemFilter filter, CancellationToken ct);

    /// <summary>
    /// Persists a new problem.
    /// </summary>
    Task AddAsync(Problem problem, CancellationToken ct);

    /// <summary>
    /// Updates an existing problem.
    /// </summary>
    Task UpdateAsync(Problem problem, CancellationToken ct);

    /// <summary>
    /// Returns the next sequential problem number for the given tenant and year.
    /// Used to generate problem numbers in the format PRB-YYYY-NNNNN.
    /// </summary>
    Task<int> GetNextProblemSequenceAsync(Guid tenantId, int year, CancellationToken ct);

    /// <summary>
    /// Returns all known errors (KEDB) for the specified tenant.
    /// </summary>
    Task<IReadOnlyList<Problem>> GetKnownErrorsAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns problems linked to a specific incident.
    /// </summary>
    Task<IReadOnlyList<Problem>> GetByLinkedIncidentIdAsync(Guid incidentId, Guid tenantId, CancellationToken ct);
}
