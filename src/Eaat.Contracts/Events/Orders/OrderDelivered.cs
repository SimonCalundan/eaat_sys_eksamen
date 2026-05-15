namespace Eaat.Contracts.Events.Orders;

public record OrderDelivered(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    DateTimeOffset DeliveredAt
) : IEvent;
