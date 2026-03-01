using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.Problems.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.ListProblems;

/// <summary>
/// Handles <see cref="ListProblemsQuery"/> by mapping filter criteria to
/// <see cref="ProblemFilter"/> and returning a paginated list of <see cref="ProblemDto"/>.
/// </summary>
public sealed class ListProblemsQueryHandler
    : IRequestHandler<ListProblemsQuery, PagedResult<ProblemDto>>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public ListProblemsQueryHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<ProblemDto>> Handle(
        ListProblemsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var filter = new ProblemFilter
        {
            TenantId = tenantId,
            Statuses = request.Statuses,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            IsKnownError = request.IsKnownError,
            SearchQuery = request.SearchQuery,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var problems = await _problemRepository.ListAsync(filter, cancellationToken);

        var items = problems
            .Select(p => new ProblemDto(
                p.Id,
                p.ProblemNumber,
                p.Title,
                p.Status,
                p.Priority,
                p.Impact,
                p.AssigneeId,
                p.IsKnownError,
                p.LinkedIncidentIds.Count,
                p.CreatedAt))
            .ToList()
            .AsReadOnly();

        // NOTE: The repository's ListAsync returns only the current page of items.
        // A proper TotalCount requires a separate CountAsync method on the repository,
        // which will be added in a future iteration.
        var totalCount = items.Count < request.PageSize && request.Page == 1
            ? items.Count
            : items.Count + ((request.Page - 1) * request.PageSize) + (items.Count == request.PageSize ? 1 : 0);

        return new PagedResult<ProblemDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);
    }
}
