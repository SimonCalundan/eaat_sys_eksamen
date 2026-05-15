using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Eaat.OrderService.Domain;
using Eaat.OrderService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eaat.OrderService.Consumers;

public sealed class OrderRejectedConsumer : EventConsumerBase<OrderRejected>
{
    private readonly ILogger<OrderRejectedConsumer> _logger;

    public OrderRejectedConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderRejectedConsumer> logger)
        : base(connection, serviceProvider, "OrderService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        OrderRejected evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(new object[] { evt.OrderId }, ct);

        if (order is null)
        {
            _logger.LogWarning("OrderRejected for unknown order {OrderId}", evt.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Placed)
        {
            _logger.LogInformation(
                "Skipping OrderRejected for {OrderId} — already in {Status}",
                evt.OrderId, order.Status);
            return;
        }

        order.Reject(evt.Reason, evt.OccurredAt);
        await db.SaveChangesAsync(ct);
    }
}
