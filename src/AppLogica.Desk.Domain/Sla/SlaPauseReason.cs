namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Predefined reasons for pausing an SLA timer.
/// Used for consistent reporting and SLA compliance auditing.
/// </summary>
public enum SlaPauseReason
{
    /// <summary>Awaiting information or action from the customer/requester.</summary>
    AwaitingCustomer = 0,

    /// <summary>Awaiting approval from a manager or change advisory board.</summary>
    AwaitingApproval = 1,

    /// <summary>Awaiting a third-party vendor to deliver a fix or information.</summary>
    AwaitingVendor = 2,

    /// <summary>Incident is pending a scheduled change window.</summary>
    ScheduledChange = 3,

    /// <summary>Incident is being handled outside business hours (manual pause).</summary>
    OutsideBusinessHours = 4,

    /// <summary>Custom reason — use the free-text PauseReason field on SlaTimer for details.</summary>
    Other = 99
}
