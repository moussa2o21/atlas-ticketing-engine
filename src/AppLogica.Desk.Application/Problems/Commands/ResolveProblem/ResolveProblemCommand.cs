using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.ResolveProblem;

/// <summary>
/// Command to resolve a problem with a root cause and optional workaround.
/// </summary>
public sealed record ResolveProblemCommand(
    Guid ProblemId,
    string RootCause,
    string? Workaround) : IRequest;
