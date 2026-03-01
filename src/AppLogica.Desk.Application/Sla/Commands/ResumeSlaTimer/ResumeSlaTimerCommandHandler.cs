using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using MediatR;

namespace AppLogica.Desk.Application.Sla.Commands.ResumeSlaTimer;

/// <summary>
/// Handles <see cref="ResumeSlaTimerCommand"/> by resuming the paused SLA timer
/// associated with the specified incident, extending due dates by the paused duration.
/// </summary>
public sealed class ResumeSlaTimerCommandHandler : IRequestHandler<ResumeSlaTimerCommand>
{
    private readonly ISlaRepository _slaRepository;
    private readonly ITenantContext _tenantContext;

    public ResumeSlaTimerCommandHandler(
        ISlaRepository slaRepository,
        ITenantContext tenantContext)
    {
        _slaRepository = slaRepository;
        _tenantContext = tenantContext;
    }

    public async Task Handle(ResumeSlaTimerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var timer = await _slaRepository.GetTimerByIncidentIdAsync(
            request.IncidentId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SLA timer for incident '{request.IncidentId}' not found for tenant '{tenantId}'.");

        timer.Resume();

        await _slaRepository.UpdateTimerAsync(timer, cancellationToken);
    }
}
