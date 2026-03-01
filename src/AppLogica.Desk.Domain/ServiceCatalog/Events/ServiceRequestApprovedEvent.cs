using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog.Events;

/// <summary>
/// Raised when a service request is approved.
/// </summary>
public sealed record ServiceRequestApprovedEvent(
    Guid RequestId,
    Guid TenantId) : IDomainEvent;
