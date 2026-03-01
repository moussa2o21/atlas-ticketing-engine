namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Lifecycle states for a service request.
/// </summary>
public enum ServiceRequestStatus
{
    Draft = 0,
    Submitted = 1,
    PendingApproval = 2,
    Approved = 3,
    InProgress = 4,
    Fulfilled = 5,
    Cancelled = 6,
    Rejected = 7
}
