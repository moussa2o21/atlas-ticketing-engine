using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.ResolveProblem;

/// <summary>
/// Handles <see cref="ResolveProblemCommand"/> by resolving the problem
/// with the provided root cause and optional workaround.
/// </summary>
public sealed class ResolveProblemCommandHandler : IRequestHandler<ResolveProblemCommand>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public ResolveProblemCommandHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(ResolveProblemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var problem = await _problemRepository.GetByIdAsync(
            request.ProblemId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Problem '{request.ProblemId}' not found for tenant '{tenantId}'.");

        problem.Resolve(request.RootCause, request.Workaround, resolvedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _problemRepository.UpdateAsync(problem, cancellationToken);
    }
}
