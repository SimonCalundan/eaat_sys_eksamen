namespace Eaat.Contracts.Events.Orders;

public record OrderAccepted(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid RestaurantId
) : IEvent;
