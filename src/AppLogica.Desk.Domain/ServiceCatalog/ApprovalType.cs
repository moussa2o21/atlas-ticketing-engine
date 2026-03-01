namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Determines how approval steps are processed within an approval workflow.
/// </summary>
public enum ApprovalType
{
    /// <summary>Steps must be completed one after another in order.</summary>
    Sequential = 0,

    /// <summary>All steps must be completed but can run concurrently.</summary>
    Parallel = 1,

    /// <summary>Only one approver from any step needs to approve.</summary>
    Any = 2
}
