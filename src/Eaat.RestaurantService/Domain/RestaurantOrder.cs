namespace Eaat.RestaurantService.Domain;

public class RestaurantOrder
{
    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string DeliveryArea { get; private set; } = default!;
    public RestaurantOrderStatus Status { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }

    private RestaurantOrder() { }

    public static RestaurantOrder Receive(
        Guid id,
        Guid restaurantId,
        Guid customerId,
        string deliveryArea,
        DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(deliveryArea))
            throw new ArgumentException("DeliveryArea is required", nameof(deliveryArea));

        return new RestaurantOrder
        {
            Id = id,
            RestaurantId = restaurantId,
            CustomerId = customerId,
            DeliveryArea = deliveryArea,
            Status = RestaurantOrderStatus.Pending,
            ReceivedAt = at,
        };
    }

    public void Accept(DateTimeOffset at)
    {
        EnsureStatus(RestaurantOrderStatus.Pending, nameof(Accept));
        Status = RestaurantOrderStatus.Accepted;
        AcceptedAt = at;
    }

    public void Reject(string reason, DateTimeOffset at)
    {
        EnsureStatus(RestaurantOrderStatus.Pending, nameof(Reject));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason required", nameof(reason));

        Status = RestaurantOrderStatus.Rejected;
        RejectionReason = reason;
        RejectedAt = at;
    }

    public void MarkReady(DateTimeOffset at)
    {
        EnsureStatus(RestaurantOrderStatus.Accepted, nameof(MarkReady));
        Status = RestaurantOrderStatus.Ready;
        ReadyAt = at;
    }

    private void EnsureStatus(RestaurantOrderStatus expected, string operation)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Cannot {operation} order {Id}: status is {Status}, expected {expected}");
    }
}
