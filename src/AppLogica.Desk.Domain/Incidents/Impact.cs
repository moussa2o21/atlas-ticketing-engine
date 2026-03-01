namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// Scope of business impact for an incident.
/// </summary>
public enum Impact
{
    Enterprise = 0,
    Department = 1,
    Team = 2,
    Individual = 3
}
