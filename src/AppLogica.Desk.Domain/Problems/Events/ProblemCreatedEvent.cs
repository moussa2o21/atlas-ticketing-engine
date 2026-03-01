using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Domain.Problems.Events;

/// <summary>
/// Raised when a new problem is created.
/// </summary>
public sealed record ProblemCreatedEvent(
    Guid ProblemId,
    Guid TenantId,
    string ProblemNumber,
    Priority Priority) : IDomainEvent;
