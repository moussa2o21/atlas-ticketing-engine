namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Tracks the approval state of a service request.
/// </summary>
public enum ApprovalStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    TimedOut = 4
}
