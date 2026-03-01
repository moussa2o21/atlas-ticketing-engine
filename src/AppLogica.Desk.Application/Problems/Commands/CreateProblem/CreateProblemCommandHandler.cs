using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Problems;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.CreateProblem;

/// <summary>
/// Handles <see cref="CreateProblemCommand"/> by creating a new problem record,
/// generating a problem number, and persisting it.
/// </summary>
public sealed class CreateProblemCommandHandler : IRequestHandler<CreateProblemCommand, Guid>
{
    private readonly IProblemRepository _problemRepository;
    private readonly ITenantContext _tenantContext;

    public CreateProblemCommandHandler(
        IProblemRepository problemRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(CreateProblemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;

        // Generate problem number: PRB-{year:D4}-{seq:D5}
        var year = now.Year;
        var sequence = await _problemRepository.GetNextProblemSequenceAsync(tenantId, year, cancellationToken);
        var problemNumber = $"PRB-{year:D4}-{sequence:D5}";

        // Create the problem aggregate via its factory method
        var problem = Problem.Create(
            tenantId,
            request.Title,
            request.Description,
            request.Priority,
            request.Impact,
            problemNumber,
            createdBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _problemRepository.AddAsync(problem, cancellationToken);

        return problem.Id;
    }
}
