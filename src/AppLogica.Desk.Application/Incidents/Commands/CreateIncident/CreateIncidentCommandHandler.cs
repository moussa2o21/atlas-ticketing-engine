using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.CreateIncident;

/// <summary>
/// Handles <see cref="CreateIncidentCommand"/> by creating a new incident,
/// generating a ticket number, attaching an SLA timer, and persisting both.
/// </summary>
public sealed class CreateIncidentCommandHandler : IRequestHandler<CreateIncidentCommand, Guid>
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ISlaRepository _slaRepository;
    private readonly ITenantContext _tenantContext;

    public CreateIncidentCommandHandler(
        IIncidentRepository incidentRepository,
        ISlaRepository slaRepository,
        ITenantContext tenantContext)
    {
        _incidentRepository = incidentRepository;
        _slaRepository = slaRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;

        // Generate ticket number: INC-{year:D4}-{seq:D5}
        var year = now.Year;
        var sequence = await _incidentRepository.GetNextTicketSequenceAsync(tenantId, year, cancellationToken);
        var ticketNumber = $"INC-{year:D4}-{sequence:D5}";

        // Create the incident aggregate via its factory method
        var incident = Incident.Create(
            tenantId,
            request.Title,
            request.Description,
            request.Impact,
            request.Urgency,
            ticketNumber,
            createdBy: Guid.Empty); // TODO: resolve from ICurrentUserContext in Phase 3

        // Find the SLA policy matching the calculated priority
        var slaPolicy = await _slaRepository.GetPolicyByPriorityAsync(
            incident.Priority, tenantId, cancellationToken);

        if (slaPolicy is not null)
        {
            var target = slaPolicy.Targets.FirstOrDefault(t => t.Priority == incident.Priority);

            if (target is not null)
            {
                var responseDueAt = now.AddMinutes(target.ResponseMinutes);
                var resolutionDueAt = now.AddMinutes(target.ResolutionMinutes);

                var slaTimer = new SlaTimer(
                    tenantId,
                    incident.Id,
                    responseDueAt,
                    resolutionDueAt);

                await _slaRepository.AddTimerAsync(slaTimer, cancellationToken);
            }
        }

        await _incidentRepository.AddAsync(incident, cancellationToken);

        return incident.Id;
    }
}
