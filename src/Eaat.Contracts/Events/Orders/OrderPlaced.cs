namespace Eaat.Contracts.Events.Orders;

public record OrderPlaced(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    Guid RestaurantId,
    string DeliveryArea
) : IEvent;
