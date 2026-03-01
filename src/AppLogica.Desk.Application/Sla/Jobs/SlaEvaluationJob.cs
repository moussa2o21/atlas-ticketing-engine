using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using AppLogica.Desk.Domain.Sla.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.Sla.Jobs;

/// <summary>
/// Background job that evaluates all active SLA timers across all tenants.
/// Designed to run on a 60-second cycle via Hangfire.
///
/// For timers at 80%+ elapsed time: marks Warning and publishes <see cref="SlaWarningEvent"/>.
/// For timers past due: marks Breached and publishes <see cref="SlaBreachedEvent"/>.
///
/// This job is idempotent — it checks the current timer status before transitioning
/// to avoid duplicate warnings or breach events.
/// </summary>
public sealed class SlaEvaluationJob
{
    private readonly ISlaRepository _slaRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<SlaEvaluationJob> _logger;

    /// <summary>
    /// The warning threshold as a percentage of elapsed time (0.80 = 80%).
    /// </summary>
    private const double WarningThreshold = 0.80;

    public SlaEvaluationJob(
        ISlaRepository slaRepository,
        IPublisher publisher,
        ILogger<SlaEvaluationJob> logger)
    {
        _slaRepository = slaRepository;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all active SLA timers for the given tenant.
    /// Called by Hangfire on a recurring schedule.
    /// </summary>
    /// <remarks>
    /// In a production multi-tenant environment, this method would be called
    /// per-tenant by iterating over all tenant IDs from a tenant registry.
    /// The Hangfire job orchestrator handles tenant enumeration.
    /// </remarks>
    public async Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SLA evaluation started for tenant {TenantId}", tenantId);

        var activeTimers = await _slaRepository.GetActiveTimersAsync(tenantId, cancellationToken);

        if (activeTimers.Count == 0)
        {
            _logger.LogDebug("No active SLA timers found for tenant {TenantId}", tenantId);
            return;
        }

        var now = DateTime.UtcNow;
        var warningCount = 0;
        var breachCount = 0;

        foreach (var timer in activeTimers)
        {
            try
            {
                await EvaluateTimerAsync(timer, now, cancellationToken);

                if (timer.Status == SlaTimerStatus.Warning) warningCount++;
                if (timer.Status == SlaTimerStatus.Breached) breachCount++;
            }
            catch (Exception ex)
            {
                // Log and continue — one timer failure should not stop evaluation of others
                _logger.LogError(ex,
                    "Failed to evaluate SLA timer {TimerId} for incident {IncidentId} in tenant {TenantId}",
                    timer.Id, timer.IncidentId, tenantId);
            }
        }

        _logger.LogInformation(
            "SLA evaluation completed for tenant {TenantId}: {Total} timers evaluated, {Warnings} warnings, {Breaches} breaches",
            tenantId, activeTimers.Count, warningCount, breachCount);
    }

    private async Task EvaluateTimerAsync(
        SlaTimer timer,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Check for breach first (resolution deadline exceeded)
        if (now >= timer.ResolutionDueAt)
        {
            // Only transition if not already breached (idempotency)
            if (timer.Status is SlaTimerStatus.Active or SlaTimerStatus.Warning)
            {
                timer.MarkBreached();
                await _slaRepository.UpdateTimerAsync(timer, cancellationToken);

                var breachedEvent = new SlaBreachedEvent(timer.Id, timer.IncidentId, timer.TenantId);
                await _publisher.Publish(breachedEvent, cancellationToken);

                _logger.LogWarning(
                    "SLA BREACHED: Timer {TimerId} for incident {IncidentId} (Tenant: {TenantId}). Resolution was due at {DueAt}",
                    timer.Id, timer.IncidentId, timer.TenantId, timer.ResolutionDueAt);
            }

            return;
        }

        // Check for warning threshold (80% of resolution time elapsed)
        if (timer.Status == SlaTimerStatus.Active)
        {
            var totalDuration = timer.ResolutionDueAt - timer.CreatedAt;
            var elapsed = now - timer.CreatedAt;

            if (totalDuration.TotalMinutes > 0)
            {
                var percentageElapsed = elapsed.TotalMinutes / totalDuration.TotalMinutes;

                if (percentageElapsed >= WarningThreshold)
                {
                    timer.MarkWarning();
                    await _slaRepository.UpdateTimerAsync(timer, cancellationToken);

                    var warningEvent = new SlaWarningEvent(timer.Id, timer.IncidentId, timer.TenantId);
                    await _publisher.Publish(warningEvent, cancellationToken);

                    _logger.LogWarning(
                        "SLA WARNING: Timer {TimerId} for incident {IncidentId} (Tenant: {TenantId}). {Percentage:P0} elapsed, due at {DueAt}",
                        timer.Id, timer.IncidentId, timer.TenantId, percentageElapsed, timer.ResolutionDueAt);
                }
            }
        }
    }
}
