using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.ResolveIncident;

/// <summary>
/// Handles <see cref="ResolveIncidentCommand"/> by resolving the incident and
/// marking the associated SLA timer as met (if it has not already been breached).
/// </summary>
public sealed class ResolveIncidentCommandHandler : IRequestHandler<ResolveIncidentCommand>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ISlaRepository _slaRepository;
    private readonly ITenantContext _tenantContext;

    public ResolveIncidentCommandHandler(
        IIncidentRepository incidentRepository,
        ISlaRepository slaRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _slaRepository = slaRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(ResolveIncidentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        incident.Resolve(request.ResolutionNotes, resolvedBy: Guid.Empty); // TODO: resolve from ICurrentUserContext

        await _incidentRepository.UpdateAsync(incident, cancellationToken);

        // Mark SLA timer as met if it has not already been breached
        var slaTimer = await _slaRepository.GetTimerByIncidentIdAsync(
            request.IncidentId, tenantId, cancellationToken);

        if (slaTimer is not null
            && slaTimer.Status is SlaTimerStatus.Active or SlaTimerStatus.Warning)
        {
            slaTimer.MarkMet();
            await _slaRepository.UpdateTimerAsync(slaTimer, cancellationToken);
        }
    }
}
