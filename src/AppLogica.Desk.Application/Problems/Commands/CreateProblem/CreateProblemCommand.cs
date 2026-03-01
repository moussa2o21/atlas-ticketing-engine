using AppLogica.Desk.Domain.Incidents;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.CreateProblem;

/// <summary>
/// Command to create a new ITIL problem record.
/// TenantId is resolved from <see cref="Common.Interfaces.ITenantContext"/>, never from the command payload.
/// </summary>
public sealed record CreateProblemCommand(
    string Title,
    string? Description,
    Priority Priority,
    Impact Impact) : IRequest<Guid>;
