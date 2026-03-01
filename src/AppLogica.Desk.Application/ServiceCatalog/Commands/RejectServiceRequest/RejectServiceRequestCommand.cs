using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.RejectServiceRequest;

/// <summary>
/// Command to reject a service request that is pending approval.
/// </summary>
public sealed record RejectServiceRequestCommand(
    Guid ServiceRequestId,
    string Reason) : IRequest;
