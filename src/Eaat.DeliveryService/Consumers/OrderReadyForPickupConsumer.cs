using Eaat.Contracts.Events.Deliveries;
using Eaat.Contracts.Events.Orders;
using Eaat.DeliveryService.Domain;
using Eaat.DeliveryService.Persistence;
using Eaat.Infra.Messaging;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eaat.DeliveryService.Consumers;

public sealed class OrderReadyForPickupConsumer : EventConsumerBase<OrderReadyForPickup>
{
    private readonly ILogger<OrderReadyForPickupConsumer> _logger;

    public OrderReadyForPickupConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderReadyForPickupConsumer> logger)
        : base(connection, serviceProvider, "DeliveryService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        OrderReadyForPickup evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<DeliveryDbContext>();
        var outboxWriter = scope.GetRequiredService<IOutboxWriter>();

        var existing = await db.Deliveries
            .FirstOrDefaultAsync(d => d.OrderId == evt.OrderId, ct);

        if (existing is not null)
        {
            _logger.LogInformation(
                "Skipping OrderReadyForPickup for {OrderId} — delivery {DeliveryId} already exists",
                evt.OrderId, existing.Id);
            return;
        }

        var delivery = Delivery.Offer(
            orderId: evt.OrderId,
            restaurantId: evt.RestaurantId,
            pickupAddress: evt.PickupAddress,
            deliveryArea: evt.DeliveryArea,
            at: evt.OccurredAt);

        db.Deliveries.Add(delivery);

        await outboxWriter.AddAsync(new DeliveryOffered(
            EventId: Guid.NewGuid(),
            CorrelationId: evt.OrderId,
            OccurredAt: delivery.OfferedAt,
            DeliveryId: delivery.Id,
            OrderId: delivery.OrderId,
            PickupAddress: delivery.PickupAddress,
            DeliveryArea: delivery.DeliveryArea
        ), ct);

        await db.SaveChangesAsync(ct);
    }
}
