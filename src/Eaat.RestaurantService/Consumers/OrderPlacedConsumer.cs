using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Eaat.RestaurantService.Domain;
using Eaat.RestaurantService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eaat.RestaurantService.Consumers;

public sealed class OrderPlacedConsumer : EventConsumerBase<OrderPlaced>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderPlacedConsumer> logger)
        : base(connection, serviceProvider, "RestaurantService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        OrderPlaced evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<RestaurantDbContext>();

        var existing = await db.Orders.FindAsync(new object[] { evt.OrderId }, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Skipping OrderPlaced for {OrderId} — already exists locally in {Status}",
                evt.OrderId, existing.Status);
            return;
        }

        var order = RestaurantOrder.Receive(
            id: evt.OrderId,
            restaurantId: evt.RestaurantId,
            customerId: evt.CustomerId,
            deliveryArea: evt.DeliveryArea,
            at: evt.OccurredAt);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
