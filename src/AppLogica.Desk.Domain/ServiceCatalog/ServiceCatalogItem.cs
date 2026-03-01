using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Represents a requestable item in the service catalog. Each item belongs
/// to a <see cref="ServiceCatalogCategory"/> and may require approval via
/// an <see cref="ApprovalWorkflow"/> before fulfillment.
/// </summary>
public sealed class ServiceCatalogItem : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public string? FulfillmentInstructions { get; private set; }
    public int ExpectedDeliveryMinutes { get; private set; }
    public bool RequiresApproval { get; private set; }
    public Guid? ApprovalWorkflowId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    // EF Core requires a parameterless constructor
    private ServiceCatalogItem() { }

    /// <summary>
    /// Creates a new service catalog item.
    /// </summary>
    public static ServiceCatalogItem Create(
        Guid tenantId,
        string name,
        Guid categoryId,
        string? description = null,
        string? fulfillmentInstructions = null,
        int expectedDeliveryMinutes = 60,
        bool requiresApproval = false,
        Guid? approvalWorkflowId = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedDeliveryMinutes, nameof(expectedDeliveryMinutes));

        if (requiresApproval && approvalWorkflowId is null)
        {
            throw new ArgumentException(
                "ApprovalWorkflowId must be provided when RequiresApproval is true.",
                nameof(approvalWorkflowId));
        }

        return new ServiceCatalogItem
        {
            TenantId = tenantId,
            Name = name,
            CategoryId = categoryId,
            Description = description,
            FulfillmentInstructions = fulfillmentInstructions,
            ExpectedDeliveryMinutes = expectedDeliveryMinutes,
            RequiresApproval = requiresApproval,
            ApprovalWorkflowId = approvalWorkflowId,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }
}
