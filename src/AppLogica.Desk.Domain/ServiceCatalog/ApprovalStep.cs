namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Value object representing a single step in an approval workflow.
/// Owned by <see cref="ApprovalWorkflow"/>.
/// </summary>
public sealed record ApprovalStep(
    int StepOrder,
    string ApproverRole,
    ApprovalType ApprovalType);
