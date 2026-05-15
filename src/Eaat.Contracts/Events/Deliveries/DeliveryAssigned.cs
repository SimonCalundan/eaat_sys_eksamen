namespace Eaat.Contracts.Events.Deliveries;

public record DeliveryAssigned(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid DeliveryId,
    Guid OrderId,
    Guid CourierId
) : IEvent;
