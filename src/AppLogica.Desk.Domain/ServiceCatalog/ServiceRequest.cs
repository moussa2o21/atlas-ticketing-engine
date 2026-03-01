using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.ServiceCatalog.Events;

namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Aggregate root representing a service request submitted by a requester
/// against a service catalog item. Enforces lifecycle state transitions
/// and raises domain events on each transition.
/// </summary>
public sealed class ServiceRequest : AggregateRoot
{
    // ───────────────────────── Properties ─────────────────────────

    public string RequestNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid CatalogItemId { get; private set; }
    public Guid RequesterId { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public ApprovalStatus ApprovalStatus { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public string? FulfillmentNotes { get; private set; }
    public DateTime? FulfilledAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // EF Core requires a parameterless constructor
    private ServiceRequest() { }

    // ───────────────────────── Factory ─────────────────────────

    /// <summary>
    /// Creates a new service request in Draft status.
    /// </summary>
    public static ServiceRequest Create(
        Guid tenantId,
        string requestNumber,
        string title,
        string? description,
        Guid catalogItemId,
        Guid requesterId,
        bool requiresApproval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestNumber, nameof(requestNumber));

        if (!requestNumber.StartsWith("SRQ-") || requestNumber.Length != 14)
        {
            throw new ArgumentException(
                "Request number must follow the format SRQ-YYYY-NNNNN.",
                nameof(requestNumber));
        }

        return new ServiceRequest
        {
            TenantId = tenantId,
            RequestNumber = requestNumber,
            Title = title,
            Description = description,
            CatalogItemId = catalogItemId,
            RequesterId = requesterId,
            Status = ServiceRequestStatus.Draft,
            ApprovalStatus = requiresApproval ? ApprovalStatus.Pending : ApprovalStatus.NotRequired,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ───────────────────────── State Transitions ─────────────────────────

    /// <summary>
    /// Submits the service request. Transitions: Draft -> Submitted (no approval)
    /// or Draft -> PendingApproval (requires approval).
    /// </summary>
    public void Submit()
    {
        GuardStatus(ServiceRequestStatus.Draft, "submit");

        if (ApprovalStatus == ApprovalStatus.Pending)
        {
            Status = ServiceRequestStatus.PendingApproval;
        }
        else
        {
            Status = ServiceRequestStatus.Submitted;
        }

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ServiceRequestSubmittedEvent(Id, TenantId, RequestNumber));
    }

    /// <summary>
    /// Approves the service request. Transitions: PendingApproval -> Approved.
    /// </summary>
    public void Approve()
    {
        GuardStatus(ServiceRequestStatus.PendingApproval, "approve");

        Status = ServiceRequestStatus.Approved;
        ApprovalStatus = ApprovalStatus.Approved;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ServiceRequestApprovedEvent(Id, TenantId));
    }

    /// <summary>
    /// Rejects the service request. Transitions: PendingApproval -> Rejected.
    /// </summary>
    public void Reject(string reason)
    {
        GuardStatus(ServiceRequestStatus.PendingApproval, "reject");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        Status = ServiceRequestStatus.Rejected;
        ApprovalStatus = ApprovalStatus.Rejected;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns the service request to a fulfillment agent.
    /// Transitions: Submitted -> InProgress, Approved -> InProgress.
    /// </summary>
    public void Assign(Guid assigneeId)
    {
        if (Status is not (ServiceRequestStatus.Submitted or ServiceRequestStatus.Approved))
        {
            throw new InvalidOperationException(
                $"Cannot assign service request in '{Status}' status. " +
                "Request must be in 'Submitted' or 'Approved' status.");
        }

        AssigneeId = assigneeId;
        Status = ServiceRequestStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the service request as fulfilled.
    /// Transitions: InProgress -> Fulfilled.
    /// </summary>
    public void Fulfill(string? notes)
    {
        GuardStatus(ServiceRequestStatus.InProgress, "fulfill");

        FulfillmentNotes = notes;
        FulfilledAt = DateTime.UtcNow;
        Status = ServiceRequestStatus.Fulfilled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ServiceRequestFulfilledEvent(Id, TenantId, RequestNumber));
    }

    /// <summary>
    /// Cancels the service request.
    /// Transitions: Draft, Submitted, PendingApproval, Approved, InProgress -> Cancelled.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status is ServiceRequestStatus.Fulfilled or ServiceRequestStatus.Cancelled or ServiceRequestStatus.Rejected)
        {
            throw new InvalidOperationException(
                $"Cannot cancel service request in '{Status}' status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        Status = ServiceRequestStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the approval as timed out. Transitions: PendingApproval -> Cancelled.
    /// </summary>
    public void TimeoutApproval()
    {
        GuardStatus(ServiceRequestStatus.PendingApproval, "timeout");

        ApprovalStatus = ApprovalStatus.TimedOut;
        Status = ServiceRequestStatus.Cancelled;
        CancellationReason = "Approval timed out.";
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // ───────────────────────── Guard Methods ─────────────────────────

    private void GuardStatus(ServiceRequestStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {action} service request in '{Status}' status. " +
                $"Request must be in '{expected}' status.");
        }
    }
}
