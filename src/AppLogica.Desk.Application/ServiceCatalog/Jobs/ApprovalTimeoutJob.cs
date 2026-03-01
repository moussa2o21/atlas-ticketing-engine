using AppLogica.Desk.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Application.ServiceCatalog.Jobs;

/// <summary>
/// Background job that marks pending approvals as TimedOut when the
/// associated approval workflow's TimeoutMinutes has been exceeded.
/// Designed to run on a recurring cycle via Hangfire.
///
/// This job is idempotent — it only transitions requests that are still in
/// PendingApproval status with a Pending approval status.
/// </summary>
public sealed class ApprovalTimeoutJob
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly ILogger<ApprovalTimeoutJob> _logger;

    public ApprovalTimeoutJob(
        IServiceRequestRepository requestRepository,
        ILogger<ApprovalTimeoutJob> logger)
    {
        _requestRepository = requestRepository;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all pending approval service requests for the given tenant
    /// and times out those that have exceeded their workflow timeout.
    /// Called by Hangfire on a recurring schedule.
    /// </summary>
    public async Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Approval timeout check started for tenant {TenantId}", tenantId);

        var timedOutRequests = await _requestRepository.GetTimedOutApprovalsAsync(tenantId, cancellationToken);

        if (timedOutRequests.Count == 0)
        {
            _logger.LogDebug("No timed-out approvals found for tenant {TenantId}", tenantId);
            return;
        }

        var timeoutCount = 0;

        foreach (var request in timedOutRequests)
        {
            try
            {
                request.TimeoutApproval();
                await _requestRepository.UpdateAsync(request, cancellationToken);
                timeoutCount++;

                _logger.LogWarning(
                    "Approval timed out for service request {RequestNumber} ({RequestId}) in tenant {TenantId}",
                    request.RequestNumber, request.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to timeout approval for service request {RequestId} in tenant {TenantId}",
                    request.Id, tenantId);
            }
        }

        _logger.LogInformation(
            "Approval timeout check completed for tenant {TenantId}: {TimedOut} of {Total} requests timed out",
            tenantId, timeoutCount, timedOutRequests.Count);
    }
}
