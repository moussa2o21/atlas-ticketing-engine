using AppLogica.Desk.Domain.Incidents;

namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Value object defining the response and resolution time targets for a given priority.
/// </summary>
public sealed record SlaTarget(
    Priority Priority,
    int ResponseMinutes,
    int ResolutionMinutes);
