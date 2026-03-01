using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.ApproveServiceRequest;

/// <summary>
/// Command to approve a service request that is pending approval.
/// </summary>
public sealed record ApproveServiceRequestCommand(
    Guid ServiceRequestId) : IRequest;
