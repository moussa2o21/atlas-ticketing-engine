namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Lifecycle states for an SLA timer.
/// </summary>
public enum SlaTimerStatus
{
    Active = 0,
    Paused = 1,
    Warning = 2,
    Breached = 3,
    Met = 4,
    Voided = 5
}
