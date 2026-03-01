using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog.Events;

/// <summary>
/// Raised when a service request is fulfilled.
/// </summary>
public sealed record ServiceRequestFulfilledEvent(
    Guid RequestId,
    Guid TenantId,
    string RequestNumber) : IDomainEvent;
