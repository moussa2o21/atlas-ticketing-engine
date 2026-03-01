using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.LinkIncidentToProblem;

/// <summary>
/// Handles <see cref="LinkIncidentToProblemCommand"/> by linking an incident
/// to the specified problem.
/// </summary>
public sealed class LinkIncidentToProblemCommandHandler : IRequestHandler<LinkIncidentToProblemCommand>
{
    private readonly IProblemRepository _problemRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public LinkIncidentToProblemCommandHandler(
        IProblemRepository problemRepository,
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _problemRepository = problemRepository;
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(LinkIncidentToProblemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var problem = await _problemRepository.GetByIdAsync(
            request.ProblemId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Problem '{request.ProblemId}' not found for tenant '{tenantId}'.");

        // Verify the incident exists
        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        problem.LinkIncident(incident.Id);

        await _problemRepository.UpdateAsync(problem, cancellationToken);
    }
}
