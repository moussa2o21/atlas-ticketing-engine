using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.AssignIncident;

/// <summary>
/// Command to assign an incident to a specific agent.
/// </summary>
public sealed record AssignIncidentCommand(
    Guid IncidentId,
    Guid AssigneeId) : IRequest;
