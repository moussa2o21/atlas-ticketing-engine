using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Sla.Commands.PauseSlaTimer;

/// <summary>
/// Handles <see cref="PauseSlaTimerCommand"/> by pausing the SLA timer associated
/// with the specified incident.
/// </summary>
public sealed class PauseSlaTimerCommandHandler : IRequestHandler<PauseSlaTimerCommand>
{
    private readonly ISlaRepository _slaRepository;
    private readonly ITenantContext _tenantContext;

    public PauseSlaTimerCommandHandler(
        ISlaRepository slaRepository,
        ITenantContext tenantContext)
    {
        _slaRepository = slaRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(PauseSlaTimerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var timer = await _slaRepository.GetTimerByIncidentIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SLA timer for incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        timer.Pause(request.Reason);

        await _slaRepository.UpdateTimerAsync(timer, cancellationToken);
    }
}
