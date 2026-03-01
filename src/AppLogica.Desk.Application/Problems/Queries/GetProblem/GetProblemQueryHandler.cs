using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Problems.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.GetProblem;

/// <summary>
/// Handles <see cref="GetProblemQuery"/> by returning a full <see cref="ProblemDetailDto"/>.
/// </summary>
public sealed class GetProblemQueryHandler : IRequestHandler<GetProblemQuery, ProblemDetailDto>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public GetProblemQueryHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ProblemDetailDto> Handle(
        GetProblemQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var problem = await _problemRepository.GetByIdAsync(
            request.ProblemId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Problem '{request.ProblemId}' not found for tenant '{tenantId}'.");

        return new ProblemDetailDto(
            problem.Id,
            problem.ProblemNumber,
            problem.Title,
            problem.Description,
            problem.Status,
            problem.Priority,
            problem.Impact,
            problem.AssigneeId,
            problem.RootCause,
            problem.Workaround,
            problem.IsKnownError,
            problem.KnownErrorPublishedAt,
            problem.ResolvedAt,
            problem.ClosedAt,
            problem.LinkedIncidentIds,
            problem.CreatedAt,
            problem.UpdatedAt);
    }
}
