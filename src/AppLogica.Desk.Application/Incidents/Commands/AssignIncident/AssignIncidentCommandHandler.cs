using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.AssignIncident;

/// <summary>
/// Handles <see cref="AssignIncidentCommand"/> by assigning the incident to the specified agent.
/// </summary>
public sealed class AssignIncidentCommandHandler : IRequestHandler<AssignIncidentCommand>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public AssignIncidentCommandHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(AssignIncidentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        incident.Assign(request.AssigneeId, assignedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
    }
}
