namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// ITIL-aligned incident lifecycle states.
/// </summary>
public enum IncidentStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    Pending = 3,
    Escalated = 4,
    Major = 5,
    Resolved = 6,
    Closed = 7
}
