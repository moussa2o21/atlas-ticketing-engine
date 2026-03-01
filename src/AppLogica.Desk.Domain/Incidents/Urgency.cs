namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// How quickly the incident needs to be resolved.
/// </summary>
public enum Urgency
{
    Immediate = 0,
    High = 1,
    Normal = 2,
    Low = 3
}
