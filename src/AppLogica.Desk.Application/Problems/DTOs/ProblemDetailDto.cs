using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;

namespace AppLogica.Desk.Application.Problems.DTOs;

/// <summary>
/// Full detail DTO for a single problem, including all fields.
/// </summary>
public sealed record ProblemDetailDto(
    Guid Id,
    string ProblemNumber,
    string Title,
    string? Description,
    ProblemStatus Status,
    Priority Priority,
    Impact Impact,
    Guid? AssigneeId,
    string? RootCause,
    string? Workaround,
    bool IsKnownError,
    DateTime? KnownErrorPublishedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    IReadOnlyList<Guid> LinkedIncidentIds,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
