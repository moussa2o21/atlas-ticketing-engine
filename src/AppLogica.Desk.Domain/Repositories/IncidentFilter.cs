using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Filter criteria for querying incidents. Used by <see cref="IIncidentRepository.ListAsync"/>.
/// </summary>
public sealed class IncidentFilter
{
    /// <summary>Required tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Filter by one or more incident statuses.</summary>
    public List<IncidentStatus>? Statuses { get; set; }

    /// <summary>Filter by one or more priority levels.</summary>
    public List<Priority>? Priorities { get; set; }

    /// <summary>Filter by assigned agent.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Filter by routing queue.</summary>
    public Guid? QueueId { get; set; }

    /// <summary>Include only incidents created on or after this date.</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>Include only incidents created on or before this date.</summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>Free-text search across ticket number, title, and description.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of results per page. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}
