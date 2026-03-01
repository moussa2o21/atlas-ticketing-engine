using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.Problems.DTOs;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.ListProblems;

/// <summary>
/// Query to list problems with filtering and pagination.
/// </summary>
public sealed record ListProblemsQuery(
    List<ProblemStatus>? Statuses,
    Priority? Priority,
    Guid? AssigneeId,
    bool? IsKnownError,
    string? SearchQuery,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProblemDto>>;
