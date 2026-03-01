using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.LinkIncidentToProblem;

/// <summary>
/// Command to link an incident to a problem.
/// </summary>
public sealed record LinkIncidentToProblemCommand(
    Guid ProblemId,
    Guid IncidentId) : IRequest;
