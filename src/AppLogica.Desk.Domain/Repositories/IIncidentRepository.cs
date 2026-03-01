using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="Incident"/> aggregate persistence.
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface IIncidentRepository
{
    /// <summary>
    /// Retrieves an incident by its ID within the specified tenant.
    /// </summary>
    Task<Incident?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns a filtered, paginated list of incidents.
    /// </summary>
    Task<IReadOnlyList<Incident>> ListAsync(IncidentFilter filter, CancellationToken ct);

    /// <summary>
    /// Persists a new incident.
    /// </summary>
    Task AddAsync(Incident incident, CancellationToken ct);

    /// <summary>
    /// Updates an existing incident.
    /// </summary>
    Task UpdateAsync(Incident incident, CancellationToken ct);

    /// <summary>
    /// Returns the next sequential ticket number for the given tenant and year.
    /// Used to generate ticket numbers in the format INC-YYYY-NNNNN.
    /// </summary>
    Task<int> GetNextTicketSequenceAsync(Guid tenantId, int year, CancellationToken ct);
}
