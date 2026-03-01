using MediatR;

namespace AppLogica.Desk.Application.Sla.Commands.PauseSlaTimer;

/// <summary>
/// Command to pause the SLA timer for an incident (e.g. when awaiting customer input).
/// </summary>
public sealed record PauseSlaTimerCommand(
    Guid IncidentId,
    string Reason) : IRequest;
