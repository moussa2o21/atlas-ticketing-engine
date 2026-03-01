using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Sla.Services;

/// <summary>
/// Application service coordinating SLA timer lifecycle with business hours calendars.
/// Handles timer creation, pause/resume, and priority change recalculation.
/// </summary>
public sealed class SlaTimerService
{
    private readonly ISlaRepository _slaRepository;
    private readonly IBusinessHoursRepository _businessHoursRepository;
    private readonly IBusinessHoursCalculator _businessHoursCalculator;
    private readonly ILogger<SlaTimerService> _logger;

    public SlaTimerService(
        ISlaRepository slaRepository,
        IBusinessHoursRepository businessHoursRepository,
        IBusinessHoursCalculator businessHoursCalculator,
        ILogger<SlaTimerService> logger)
    {
        _slaRepository = slaRepository;
        _businessHoursRepository = businessHoursRepository;
        _businessHoursCalculator = businessHoursCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new SLA timer for an incident using the tenant's default business hours calendar
    /// to compute deadline dates in business minutes.
    /// </summary>
    public async Task<SlaTimer?> CreateTimerAsync(
        Guid incidentId,
        Priority priority,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var policy = await _slaRepository.GetPolicyByPriorityAsync(priority, tenantId, ct);
        if (policy is null)
        {
            _logger.LogWarning(
                "No SLA policy found for priority {Priority} in tenant {TenantId}. Skipping timer creation.",
                priority, tenantId);
            return null;
        }

        var target = policy.Targets.FirstOrDefault(t => t.Priority == priority);
        if (target is null)
        {
            _logger.LogWarning("SLA policy '{PolicyName}' has no target for priority {Priority}.", policy.Name, priority);
            return null;
        }

        var calendar = await _businessHoursRepository.GetDefaultCalendarAsync(tenantId, ct);
        var now = DateTime.UtcNow;

        DateTime responseDueAt;
        DateTime resolutionDueAt;

        if (calendar is not null)
        {
            responseDueAt = await _businessHoursCalculator.CalculateDeadlineAsync(
                now, target.ResponseMinutes, calendar.Id, tenantId, ct);
            resolutionDueAt = await _businessHoursCalculator.CalculateDeadlineAsync(
                now, target.ResolutionMinutes, calendar.Id, tenantId, ct);
        }
        else
        {
            // Fallback: no business hours calendar, use raw minutes (24/7)
            responseDueAt = now.AddMinutes(target.ResponseMinutes);
            resolutionDueAt = now.AddMinutes(target.ResolutionMinutes);
        }

        var timer = new SlaTimer(tenantId, incidentId, responseDueAt, resolutionDueAt);
        await _slaRepository.AddTimerAsync(timer, ct);

        _logger.LogInformation(
            "SLA timer created for incident {IncidentId}: response due {ResponseDue}, resolution due {ResolutionDue}",
            incidentId, responseDueAt, resolutionDueAt);

        return timer;
    }

    /// <summary>
    /// Pauses the SLA timer for an incident with a structured reason.
    /// </summary>
    public async Task PauseTimerAsync(
        Guid incidentId,
        Guid tenantId,
        SlaPauseReason reason,
        string? details = null,
        CancellationToken ct = default)
    {
        var timer = await _slaRepository.GetTimerByIncidentIdAsync(incidentId, tenantId, ct);
        if (timer is null)
        {
            _logger.LogWarning("No SLA timer found for incident {IncidentId}.", incidentId);
            return;
        }

        var reasonText = details is not null ? $"{reason}: {details}" : reason.ToString();
        timer.Pause(reasonText);
        await _slaRepository.UpdateTimerAsync(timer, ct);

        _logger.LogInformation("SLA timer paused for incident {IncidentId}: {Reason}", incidentId, reasonText);
    }

    /// <summary>
    /// Resumes a paused SLA timer, extending deadlines by the paused duration.
    /// </summary>
    public async Task ResumeTimerAsync(
        Guid incidentId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var timer = await _slaRepository.GetTimerByIncidentIdAsync(incidentId, tenantId, ct);
        if (timer is null)
        {
            _logger.LogWarning("No SLA timer found for incident {IncidentId}.", incidentId);
            return;
        }

        timer.Resume();
        await _slaRepository.UpdateTimerAsync(timer, ct);

        _logger.LogInformation(
            "SLA timer resumed for incident {IncidentId}: new resolution due {ResolutionDue}",
            incidentId, timer.ResolutionDueAt);
    }

    /// <summary>
    /// Recalculates SLA deadlines when an incident's priority changes.
    /// Preserves elapsed business time and computes new deadlines from the new priority's targets.
    /// </summary>
    public async Task RecalculateOnPriorityChangeAsync(
        Guid incidentId,
        Priority newPriority,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var timer = await _slaRepository.GetTimerByIncidentIdAsync(incidentId, tenantId, ct);
        if (timer is null)
        {
            _logger.LogWarning("No SLA timer found for incident {IncidentId} during priority recalculation.", incidentId);
            return;
        }

        if (timer.Status is SlaTimerStatus.Breached or SlaTimerStatus.Met or SlaTimerStatus.Voided)
        {
            _logger.LogInformation("SLA timer for incident {IncidentId} is in terminal state {Status}. Skipping recalculation.",
                incidentId, timer.Status);
            return;
        }

        var policy = await _slaRepository.GetPolicyByPriorityAsync(newPriority, tenantId, ct);
        if (policy is null)
        {
            _logger.LogWarning("No SLA policy found for new priority {Priority}. Timer unchanged.", newPriority);
            return;
        }

        var target = policy.Targets.FirstOrDefault(t => t.Priority == newPriority);
        if (target is null) return;

        var calendar = await _businessHoursRepository.GetDefaultCalendarAsync(tenantId, ct);
        var now = DateTime.UtcNow;

        // Calculate elapsed business minutes since timer creation
        int elapsedBusinessMinutes = 0;
        if (calendar is not null)
        {
            elapsedBusinessMinutes = await _businessHoursCalculator.GetBusinessMinutesBetweenAsync(
                timer.CreatedAt, now, calendar.Id, tenantId, ct);
        }
        else
        {
            elapsedBusinessMinutes = (int)(now - timer.CreatedAt).TotalMinutes;
        }

        // Void the old timer and create a new one with adjusted deadlines
        timer.MarkVoided();
        await _slaRepository.UpdateTimerAsync(timer, ct);

        var remainingResponseMinutes = Math.Max(0, target.ResponseMinutes - elapsedBusinessMinutes);
        var remainingResolutionMinutes = Math.Max(0, target.ResolutionMinutes - elapsedBusinessMinutes);

        DateTime responseDueAt;
        DateTime resolutionDueAt;

        if (calendar is not null)
        {
            responseDueAt = await _businessHoursCalculator.CalculateDeadlineAsync(
                now, remainingResponseMinutes, calendar.Id, tenantId, ct);
            resolutionDueAt = await _businessHoursCalculator.CalculateDeadlineAsync(
                now, remainingResolutionMinutes, calendar.Id, tenantId, ct);
        }
        else
        {
            responseDueAt = now.AddMinutes(remainingResponseMinutes);
            resolutionDueAt = now.AddMinutes(remainingResolutionMinutes);
        }

        var newTimer = new SlaTimer(tenantId, incidentId, responseDueAt, resolutionDueAt);
        await _slaRepository.AddTimerAsync(newTimer, ct);

        _logger.LogInformation(
            "SLA timer recalculated for incident {IncidentId} (priority → {NewPriority}): " +
            "elapsed {Elapsed} biz min, new resolution due {ResolutionDue}",
            incidentId, newPriority, elapsedBusinessMinutes, resolutionDueAt);
    }
}
