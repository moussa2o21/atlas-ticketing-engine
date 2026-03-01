using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Problems.Events;

/// <summary>
/// Raised when a problem is resolved.
/// </summary>
public sealed record ProblemResolvedEvent(
    Guid ProblemId,
    Guid TenantId,
    string ProblemNumber) : IDomainEvent;
