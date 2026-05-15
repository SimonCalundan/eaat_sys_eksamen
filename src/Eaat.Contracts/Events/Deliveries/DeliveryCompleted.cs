namespace Eaat.Contracts.Events.Deliveries;

public record DeliveryCompleted(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid DeliveryId,
    Guid OrderId,
    Guid CourierId,
    DateTimeOffset CompletedAt
) : IEvent;
