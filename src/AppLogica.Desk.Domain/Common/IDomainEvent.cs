using MediatR;

namespace AppLogica.Desk.Domain.Common;

/// <summary>
/// Marker interface for domain events. All domain events implement this
/// so they can be dispatched through MediatR's notification pipeline.
/// </summary>
public interface IDomainEvent : INotification;
