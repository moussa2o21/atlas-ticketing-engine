using AppLogica.Desk.Domain.ServiceCatalog;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Filter criteria for querying service requests.
/// Used by <see cref="IServiceRequestRepository.ListAsync"/>.
/// </summary>
public sealed class ServiceRequestFilter
{
    /// <summary>Required tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Filter by one or more service request statuses.</summary>
    public List<ServiceRequestStatus>? Statuses { get; set; }

    /// <summary>Filter by the requester who created the request.</summary>
    public Guid? RequesterId { get; set; }

    /// <summary>Filter by catalog item.</summary>
    public Guid? CatalogItemId { get; set; }

    /// <summary>Filter by assigned fulfillment agent.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of results per page. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}
