using AppLogica.Desk.Domain.Incidents;
using MediatR;

namespace AppLogica.Desk.Application.Incidents.Commands.CreateIncident;

/// <summary>
/// Command to create a new ITIL incident.
/// TenantId is resolved from <see cref="Common.Interfaces.ITenantContext"/>, never from the command payload.
/// </summary>
public sealed record CreateIncidentCommand(
    string Title,
    string Description,
    Impact Impact,
    Urgency Urgency,
    Guid? QueueId) : IRequest<Guid>;
