using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;

namespace AppLogica.Desk.Application.Problems.DTOs;

/// <summary>
/// Summary DTO for problem list views.
/// </summary>
public sealed record ProblemDto(
    Guid Id,
    string ProblemNumber,
    string Title,
    ProblemStatus Status,
    Priority Priority,
    Impact Impact,
    Guid? AssigneeId,
    bool IsKnownError,
    int LinkedIncidentCount,
    DateTime CreatedAt);
