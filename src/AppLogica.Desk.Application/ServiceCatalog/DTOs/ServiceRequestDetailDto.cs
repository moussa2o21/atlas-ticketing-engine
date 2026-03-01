using AppLogica.Desk.Domain.ServiceCatalog;

namespace AppLogica.Desk.Application.ServiceCatalog.DTOs;

/// <summary>
/// Full detail DTO for a single service request.
/// </summary>
public sealed record ServiceRequestDetailDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string? Description,
    ServiceRequestStatus Status,
    ApprovalStatus ApprovalStatus,
    Guid CatalogItemId,
    Guid RequesterId,
    Guid? AssigneeId,
    string? FulfillmentNotes,
    DateTime? FulfilledAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
