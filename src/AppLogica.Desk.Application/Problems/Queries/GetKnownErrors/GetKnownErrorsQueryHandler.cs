using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Problems.DTOs;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.GetKnownErrors;

/// <summary>
/// Handles <see cref="GetKnownErrorsQuery"/> by returning all Known Error Database entries.
/// </summary>
public sealed class GetKnownErrorsQueryHandler
    : IRequestHandler<GetKnownErrorsQuery, IReadOnlyList<KnownErrorDto>>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public GetKnownErrorsQueryHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<KnownErrorDto>> Handle(
        GetKnownErrorsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var knownErrors = await _problemRepository.GetKnownErrorsAsync(tenantId, cancellationToken);

        return knownErrors
            .Select(p => new KnownErrorDto(
                p.Id,
                p.ProblemNumber,
                p.Title,
                p.Description,
                p.Priority,
                p.Impact,
                p.RootCause,
                p.Workaround,
                p.KnownErrorPublishedAt,
                p.LinkedIncidentIds.Count))
            .ToList()
            .AsReadOnly();
    }
}
