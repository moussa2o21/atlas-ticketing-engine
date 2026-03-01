namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// Distinguishes standard incidents from major incidents.
/// </summary>
public enum IncidentType
{
    Incident = 0,
    MajorIncident = 1
}
