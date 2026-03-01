using MediatR;

namespace AppLogica.Desk.Application.Sla.Commands.ResumeSlaTimer;

/// <summary>
/// Command to resume a paused SLA timer for an incident.
/// </summary>
public sealed record ResumeSlaTimerCommand(
    Guid IncidentId) : IRequest;
