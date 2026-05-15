namespace Eaat.Contracts.Events.Orders;

public record OrderReadyForPickup(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid RestaurantId,
    string PickupAddress,
    string DeliveryArea
) : IEvent;
