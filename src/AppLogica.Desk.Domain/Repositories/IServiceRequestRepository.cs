using AppLogica.Desk.Domain.ServiceCatalog;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="ServiceRequest"/> aggregate persistence.
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface IServiceRequestRepository
{
    /// <summary>
    /// Retrieves a service request by its ID within the specified tenant.
    /// </summary>
    Task<ServiceRequest?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Returns a filtered, paginated list of service requests.
    /// </summary>
    Task<IReadOnlyList<ServiceRequest>> ListAsync(ServiceRequestFilter filter, CancellationToken ct);

    /// <summary>
    /// Persists a new service request.
    /// </summary>
    Task AddAsync(ServiceRequest request, CancellationToken ct);

    /// <summary>
    /// Updates an existing service request.
    /// </summary>
    Task UpdateAsync(ServiceRequest request, CancellationToken ct);

    /// <summary>
    /// Returns the next sequential request number for the given tenant and year.
    /// Used to generate request numbers in the format SRQ-YYYY-NNNNN.
    /// </summary>
    Task<int> GetNextRequestSequenceAsync(Guid tenantId, int year, CancellationToken ct);

    /// <summary>
    /// Returns all service requests in PendingApproval status that have exceeded
    /// their approval timeout, based on the associated approval workflow's TimeoutMinutes.
    /// </summary>
    Task<IReadOnlyList<ServiceRequest>> GetTimedOutApprovalsAsync(Guid tenantId, CancellationToken ct);
}
