namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// Incident priority calculated from the ITIL Impact x Urgency matrix.
/// </summary>
public enum Priority
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3
}
