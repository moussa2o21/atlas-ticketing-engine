using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.EscalateIncident;

/// <summary>
/// Command to escalate an incident with a reason.
/// </summary>
public sealed record EscalateIncidentCommand(
    Guid IncidentId,
    string Reason) : IRequest;
