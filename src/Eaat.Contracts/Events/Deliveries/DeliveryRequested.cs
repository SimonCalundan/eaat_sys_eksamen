namespace Eaat.Contracts.Events.Deliveries;

public record DeliveryRequested(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid RestaurantId,
    string PickupAddress,
    string DeliveryArea
) : IEvent;
