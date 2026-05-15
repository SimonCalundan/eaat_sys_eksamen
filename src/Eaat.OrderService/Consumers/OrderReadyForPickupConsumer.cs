using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Eaat.OrderService.Domain;
using Eaat.OrderService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eaat.OrderService.Consumers;

public sealed class OrderReadyForPickupConsumer : EventConsumerBase<OrderReadyForPickup>
{
    private readonly ILogger<OrderReadyForPickupConsumer> _logger;

    public OrderReadyForPickupConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderReadyForPickupConsumer> logger)
        : base(connection, serviceProvider, "OrderService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        OrderReadyForPickup evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(new object[] { evt.OrderId }, ct);

        if (order is null)
        {
            _logger.LogWarning("OrderReadyForPickup for unknown order {OrderId}", evt.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Accepted)
        {
            _logger.LogInformation(
                "Skipping OrderReadyForPickup for {OrderId} — already in {Status}",
                evt.OrderId, order.Status);
            return;
        }

        order.MarkReady(evt.OccurredAt);
        await db.SaveChangesAsync(ct);
    }
}
