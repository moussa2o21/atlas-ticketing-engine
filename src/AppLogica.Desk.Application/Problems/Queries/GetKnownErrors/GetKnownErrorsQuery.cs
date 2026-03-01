using AppLogica.Desk.Application.Problems.DTOs;
using MediatR;

namespace AppLogica.Desk.Application.Problems.Queries.GetKnownErrors;

/// <summary>
/// Query to retrieve all Known Errors from the KEDB.
/// </summary>
public sealed record GetKnownErrorsQuery : IRequest<IReadOnlyList<KnownErrorDto>>;
