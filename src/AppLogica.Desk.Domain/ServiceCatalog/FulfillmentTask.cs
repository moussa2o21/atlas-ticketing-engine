using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Represents an individual fulfillment task within a service request.
/// Multiple tasks can be created per request to track parallel work items.
/// </summary>
public sealed class FulfillmentTask : Entity
{
    public Guid ServiceRequestId { get; private set; }
    public string Title { get; private set; } = default!;
    public Guid? AssigneeId { get; private set; }
    public FulfillmentTaskStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public string? Notes { get; private set; }

    // EF Core requires a parameterless constructor
    private FulfillmentTask() { }

    /// <summary>
    /// Creates a new fulfillment task.
    /// </summary>
    public static FulfillmentTask Create(
        Guid tenantId,
        Guid serviceRequestId,
        string title,
        Guid? assigneeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        return new FulfillmentTask
        {
            TenantId = tenantId,
            ServiceRequestId = serviceRequestId,
            Title = title,
            AssigneeId = assigneeId,
            Status = FulfillmentTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Starts progress on this task.
    /// </summary>
    public void StartProgress()
    {
        if (Status is not FulfillmentTaskStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot start progress on task in '{Status}' status. Task must be in 'Pending' status.");
        }

        Status = FulfillmentTaskStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this task as completed.
    /// </summary>
    public void Complete(Guid completedBy, string? notes = null)
    {
        if (Status is not (FulfillmentTaskStatus.Pending or FulfillmentTaskStatus.InProgress))
        {
            throw new InvalidOperationException(
                $"Cannot complete task in '{Status}' status. Task must be in 'Pending' or 'InProgress' status.");
        }

        Status = FulfillmentTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Skips this task.
    /// </summary>
    public void Skip(Guid skippedBy, string? reason = null)
    {
        if (Status is not (FulfillmentTaskStatus.Pending or FulfillmentTaskStatus.InProgress))
        {
            throw new InvalidOperationException(
                $"Cannot skip task in '{Status}' status. Task must be in 'Pending' or 'InProgress' status.");
        }

        Status = FulfillmentTaskStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = skippedBy;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
