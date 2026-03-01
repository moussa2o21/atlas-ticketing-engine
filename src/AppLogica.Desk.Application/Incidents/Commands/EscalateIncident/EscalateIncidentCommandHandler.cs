using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.EscalateIncident;

/// <summary>
/// Handles <see cref="EscalateIncidentCommand"/> by escalating the incident.
/// </summary>
public sealed class EscalateIncidentCommandHandler : IRequestHandler<EscalateIncidentCommand>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public EscalateIncidentCommandHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(EscalateIncidentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        incident.Escalate(request.Reason, escalatedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
    }
}
