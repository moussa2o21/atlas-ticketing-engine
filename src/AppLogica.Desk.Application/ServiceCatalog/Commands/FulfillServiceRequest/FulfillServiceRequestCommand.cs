using MediatR;

namespace AppLogica.Desk.Application.ServiceCatalog.Commands.FulfillServiceRequest;

/// <summary>
/// Command to mark a service request as fulfilled.
/// </summary>
public sealed record FulfillServiceRequestCommand(
    Guid ServiceRequestId,
    string? FulfillmentNotes) : IRequest;
