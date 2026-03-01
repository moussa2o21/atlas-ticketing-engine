using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.ServiceCatalog.Events;

/// <summary>
/// Raised when a service request is submitted.
/// </summary>
public sealed record ServiceRequestSubmittedEvent(
    Guid RequestId,
    Guid TenantId,
    string RequestNumber) : IDomainEvent;
