using MediatR;

namespace AppLogica.Desk.Application.Problems.Commands.PublishKnownError;

/// <summary>
/// Command to publish a problem as a Known Error to the KEDB.
/// </summary>
public sealed record PublishKnownErrorCommand(
    Guid ProblemId,
    string Workaround) : IRequest;
