namespace AppLogica.Desk.Application.ServiceCatalog.DTOs;

/// <summary>
/// DTO for a service catalog item.
/// </summary>
public sealed record ServiceCatalogItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string? FulfillmentInstructions,
    int ExpectedDeliveryMinutes,
    bool RequiresApproval,
    Guid? ApprovalWorkflowId,
    int SortOrder,
    bool IsActive);
