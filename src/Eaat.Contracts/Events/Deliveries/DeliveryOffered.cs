namespace Eaat.Contracts.Events.Deliveries;

public record DeliveryOffered(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid DeliveryId,
    Guid OrderId,
    string PickupAddress,
    string DeliveryArea
) : IEvent;
