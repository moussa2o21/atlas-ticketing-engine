using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.ResolveIncident;

/// <summary>
/// Command to resolve an incident with resolution notes.
/// </summary>
public sealed record ResolveIncidentCommand(
    Guid IncidentId,
    string ResolutionNotes) : IRequest;
