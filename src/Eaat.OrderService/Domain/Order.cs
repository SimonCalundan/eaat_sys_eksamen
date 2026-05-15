namespace Eaat.OrderService.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid RestaurantId { get; private set; }
    public string DeliveryArea { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private Order() { }

    public static Order Place(Guid customerId, Guid restaurantId, string deliveryArea)
    {
        if (string.IsNullOrWhiteSpace(deliveryArea))
            throw new ArgumentException("DeliveryArea is required", nameof(deliveryArea));

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            RestaurantId = restaurantId,
            DeliveryArea = deliveryArea,
            Status = OrderStatus.Placed,
            PlacedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Accept(DateTimeOffset at)
    {
        EnsureStatus(OrderStatus.Placed, nameof(Accept));
        Status = OrderStatus.Accepted;
        AcceptedAt = at;
    }

    public void Reject(string reason, DateTimeOffset at)
    {
        EnsureStatus(OrderStatus.Placed, nameof(Reject));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason required", nameof(reason));

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        RejectedAt = at;
    }

    public void MarkReady(DateTimeOffset at)
    {
        EnsureStatus(OrderStatus.Accepted, nameof(MarkReady));
        Status = OrderStatus.Ready;
        ReadyAt = at;
    }

    public void MarkPickedUp(DateTimeOffset at)
    {
        EnsureStatus(OrderStatus.Ready, nameof(MarkPickedUp));
        Status = OrderStatus.PickedUp;
        PickedUpAt = at;
    }

    public void MarkDelivered(DateTimeOffset at)
    {
        EnsureStatus(OrderStatus.PickedUp, nameof(MarkDelivered));
        Status = OrderStatus.Delivered;
        DeliveredAt = at;
    }

    private void EnsureStatus(OrderStatus expected, string operation)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Cannot {operation} order {Id}: status is {Status}, expected {expected}");
    }
}
