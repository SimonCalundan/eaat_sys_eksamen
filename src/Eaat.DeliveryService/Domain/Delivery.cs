namespace Eaat.DeliveryService.Domain;

public class Delivery
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid RestaurantId { get; private set; }
    public string PickupAddress { get; private set; } = default!;
    public string DeliveryArea { get; private set; } = default!;
    public DeliveryStatus Status { get; private set; }
    public Guid? CourierId { get; private set; }
    public DateTimeOffset OfferedAt { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Delivery() { }

    public static Delivery Offer(
        Guid orderId,
        Guid restaurantId,
        string pickupAddress,
        string deliveryArea,
        DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(pickupAddress))
            throw new ArgumentException("PickupAddress is required", nameof(pickupAddress));
        if (string.IsNullOrWhiteSpace(deliveryArea))
            throw new ArgumentException("DeliveryArea is required", nameof(deliveryArea));

        return new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            RestaurantId = restaurantId,
            PickupAddress = pickupAddress,
            DeliveryArea = deliveryArea,
            Status = DeliveryStatus.Available,
            OfferedAt = at,
        };
    }

    public void Complete(Guid courierId, DateTimeOffset at)
    {
        if (Status != DeliveryStatus.Assigned)
            throw new InvalidOperationException(
                $"Cannot complete delivery {Id}: status is {Status}, expected Assigned");

        if (CourierId != courierId)
            throw new InvalidOperationException(
                $"Cannot complete delivery {Id}: claimed by courier {CourierId}, not {courierId}");

        Status = DeliveryStatus.Completed;
        CompletedAt = at;
    }
}
