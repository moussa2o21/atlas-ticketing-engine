using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.CloseIncident;

/// <summary>
/// Handles <see cref="CloseIncidentCommand"/> by closing a resolved incident.
/// </summary>
public sealed class CloseIncidentCommandHandler : IRequestHandler<CloseIncidentCommand>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITenantContext _tenantContext;

    public CloseIncidentCommandHandler(
        IIncidentRepository incidentRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(CloseIncidentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        incident.Close(closedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
    }
}
