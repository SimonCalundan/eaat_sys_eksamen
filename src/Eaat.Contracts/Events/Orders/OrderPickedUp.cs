namespace Eaat.Contracts.Events.Orders;

public record OrderPickedUp(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    Guid CourierId,
    DateTimeOffset PickedUpAt
) : IEvent;
