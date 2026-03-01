using AppLogica.Desk.Domain.ServiceCatalog;

namespace AppLogica.Desk.Application.ServiceCatalog.DTOs;

/// <summary>
/// Summary DTO for service request list views.
/// </summary>
public sealed record ServiceRequestDto(
    Guid Id,
    string RequestNumber,
    string Title,
    ServiceRequestStatus Status,
    ApprovalStatus ApprovalStatus,
    Guid CatalogItemId,
    Guid RequesterId,
    Guid? AssigneeId,
    DateTime CreatedAt);
