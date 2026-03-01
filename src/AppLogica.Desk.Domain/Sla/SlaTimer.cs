using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.Sla.Events;

namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Tracks the SLA response and resolution deadlines for a specific incident.
/// Supports pause/resume for pending states and raises domain events on warning and breach.
/// </summary>
public sealed class SlaTimer : Entity
{
    public Guid IncidentId { get; private set; }
    public DateTime ResponseDueAt { get; private set; }
    public DateTime ResolutionDueAt { get; private set; }
    public SlaTimerStatus Status { get; private set; }
    public string? PauseReason { get; private set; }
    public DateTime? PausedAt { get; private set; }
    public TimeSpan? ElapsedBeforePause { get; private set; }

    // EF Core requires a parameterless constructor
    private SlaTimer() { }

    /// <summary>
    /// Creates a new active SLA timer for the given incident.
    /// </summary>
    public SlaTimer(
        Guid tenantId,
        Guid incidentId,
        DateTime responseDueAt,
        DateTime resolutionDueAt)
    {
        TenantId = tenantId;
        IncidentId = incidentId;
        ResponseDueAt = responseDueAt;
        ResolutionDueAt = resolutionDueAt;
        Status = SlaTimerStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the SLA timer (e.g. when the incident enters Pending status awaiting customer input).
    /// Records the elapsed time so it can be resumed accurately.
    /// </summary>
    public void Pause(string reason)
    {
        if (Status is not (SlaTimerStatus.Active or SlaTimerStatus.Warning))
        {
            throw new InvalidOperationException(
                $"Cannot pause SLA timer in '{Status}' status. Timer must be 'Active' or 'Warning'.");
        }

        var now = DateTime.UtcNow;
        ElapsedBeforePause = now - CreatedAt;
        PausedAt = now;
        PauseReason = reason;
        Status = SlaTimerStatus.Paused;
        UpdatedAt = now;
    }

    /// <summary>
    /// Resumes a paused SLA timer, extending the due dates by the paused duration.
    /// </summary>
    public void Resume()
    {
        if (Status is not SlaTimerStatus.Paused)
        {
            throw new InvalidOperationException(
                $"Cannot resume SLA timer in '{Status}' status. Timer must be 'Paused'.");
        }

        if (PausedAt is null)
        {
            throw new InvalidOperationException("Cannot resume: PausedAt is not set.");
        }

        var now = DateTime.UtcNow;
        var pausedDuration = now - PausedAt.Value;

        ResponseDueAt = ResponseDueAt.Add(pausedDuration);
        ResolutionDueAt = ResolutionDueAt.Add(pausedDuration);

        PausedAt = null;
        PauseReason = null;
        ElapsedBeforePause = null;
        Status = SlaTimerStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>
    /// Transitions the timer to Warning status when breach is imminent.
    /// Raises an <see cref="SlaWarningEvent"/>.
    /// </summary>
    public void MarkWarning()
    {
        if (Status is not SlaTimerStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot mark SLA timer as warning in '{Status}' status. Timer must be 'Active'.");
        }

        Status = SlaTimerStatus.Warning;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the timer to Breached status when a deadline has been missed.
    /// Raises an <see cref="SlaBreachedEvent"/>.
    /// </summary>
    public void MarkBreached()
    {
        if (Status is not (SlaTimerStatus.Active or SlaTimerStatus.Warning))
        {
            throw new InvalidOperationException(
                $"Cannot mark SLA timer as breached in '{Status}' status. Timer must be 'Active' or 'Warning'.");
        }

        Status = SlaTimerStatus.Breached;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the timer to Met status when the incident is resolved within SLA.
    /// </summary>
    public void MarkMet()
    {
        if (Status is not (SlaTimerStatus.Active or SlaTimerStatus.Warning))
        {
            throw new InvalidOperationException(
                $"Cannot mark SLA timer as met in '{Status}' status. Timer must be 'Active' or 'Warning'.");
        }

        Status = SlaTimerStatus.Met;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Voids the timer when the SLA target is recalculated (e.g. after a priority change).
    /// The voided timer is preserved for audit trail; a new timer replaces it.
    /// </summary>
    public void MarkVoided()
    {
        if (Status is SlaTimerStatus.Voided)
        {
            throw new InvalidOperationException("SLA timer is already voided.");
        }

        Status = SlaTimerStatus.Voided;
        UpdatedAt = DateTime.UtcNow;
    }
}
