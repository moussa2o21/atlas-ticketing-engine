using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Filter criteria for querying problems. Used by <see cref="IProblemRepository.ListAsync"/>.
/// </summary>
public sealed class ProblemFilter
{
    /// <summary>Required tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Filter by one or more problem statuses.</summary>
    public List<ProblemStatus>? Statuses { get; set; }

    /// <summary>Filter by priority level.</summary>
    public Priority? Priority { get; set; }

    /// <summary>Filter by assigned investigator.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Filter to known errors only.</summary>
    public bool? IsKnownError { get; set; }

    /// <summary>Free-text search across problem number, title, and description.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of results per page. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}
