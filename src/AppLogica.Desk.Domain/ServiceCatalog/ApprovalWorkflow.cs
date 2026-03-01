using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Defines a multi-step approval workflow that can be associated with
/// service catalog items requiring approval before fulfillment.
/// </summary>
public sealed class ApprovalWorkflow : Entity
{
    private readonly List<ApprovalStep> _steps = [];

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public IReadOnlyList<ApprovalStep> Steps => _steps.AsReadOnly();
    public int TimeoutMinutes { get; private set; }

    // EF Core requires a parameterless constructor
    private ApprovalWorkflow() { }

    /// <summary>
    /// Creates a new approval workflow.
    /// </summary>
    public static ApprovalWorkflow Create(
        Guid tenantId,
        string name,
        string? description,
        int timeoutMinutes,
        IEnumerable<ApprovalStep> steps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutMinutes, nameof(timeoutMinutes));

        var workflow = new ApprovalWorkflow
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            TimeoutMinutes = timeoutMinutes,
            CreatedAt = DateTime.UtcNow
        };

        workflow._steps.AddRange(steps);

        return workflow;
    }
}
