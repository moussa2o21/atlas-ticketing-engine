using AppLogica.Desk.Application.Problems.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.GetProblem;

/// <summary>
/// Query to retrieve the full detail of a single problem.
/// </summary>
public sealed record GetProblemQuery(
    Guid ProblemId) : IRequest<ProblemDetailDto>;
