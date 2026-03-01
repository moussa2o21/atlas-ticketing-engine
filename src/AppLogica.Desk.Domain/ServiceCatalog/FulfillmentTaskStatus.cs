namespace AppLogica.Desk.Domain.ServiceCatalog;

/// <summary>
/// Status of a fulfillment task within a service request.
/// </summary>
public enum FulfillmentTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3
}
