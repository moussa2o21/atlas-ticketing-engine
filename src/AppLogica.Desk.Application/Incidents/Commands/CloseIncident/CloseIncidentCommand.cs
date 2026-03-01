using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.CloseIncident;

/// <summary>
/// Command to close a resolved incident.
/// </summary>
public sealed record CloseIncidentCommand(
    Guid IncidentId) : IRequest;
