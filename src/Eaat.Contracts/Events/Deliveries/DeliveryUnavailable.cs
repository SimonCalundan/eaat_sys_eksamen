namespace Eaat.Contracts.Events.Deliveries;

public record DeliveryUnavailable(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid DeliveryId,
    Guid OrderId
) : IEvent;
