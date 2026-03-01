using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Application.Problems.DTOs;

/// <summary>
/// DTO for Known Error Database (KEDB) entries.
/// </summary>
public sealed record KnownErrorDto(
    Guid Id,
    string ProblemNumber,
    string Title,
    string? Description,
    Priority Priority,
    Impact Impact,
    string? RootCause,
    string? Workaround,
    DateTime? KnownErrorPublishedAt,
    int LinkedIncidentCount);
