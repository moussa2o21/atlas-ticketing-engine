using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.PublishKnownError;

/// <summary>
/// Handles <see cref="PublishKnownErrorCommand"/> by publishing the problem
/// as a Known Error to the KEDB with the provided workaround.
/// </summary>
public sealed class PublishKnownErrorCommandHandler : IRequestHandler<PublishKnownErrorCommand>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public PublishKnownErrorCommandHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(PublishKnownErrorCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var problem = await _problemRepository.GetByIdAsync(
            request.ProblemId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Problem '{request.ProblemId}' not found for tenant '{tenantId}'.");

        problem.PublishAsKnownError(request.Workaround, publishedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _problemRepository.UpdateAsync(problem, cancellationToken);
    }
}
