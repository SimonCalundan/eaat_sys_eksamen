namespace Eaat.Contracts.Events.Orders;

public record OrderRejected(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid RestaurantId,
    string Reason
) : IEvent;
