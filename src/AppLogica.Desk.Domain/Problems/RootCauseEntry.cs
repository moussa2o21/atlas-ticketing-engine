namespace AppLogica.Desk.Domain.Problems;

/// <summary>
/// Value object representing a structured root cause analysis entry.
/// Stored as JSONB in the database within the Problem aggregate.
/// </summary>
public sealed record RootCauseEntry(
    string Category,
    string Description,
    DateTime IdentifiedAt,
    Guid IdentifiedBy);
