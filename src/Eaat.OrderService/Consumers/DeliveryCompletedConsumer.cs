using Eaat.Contracts.Events.Deliveries;
using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Eaat.Infra.Outbox;
using Eaat.OrderService.Domain;
using Eaat.OrderService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eaat.OrderService.Consumers;

public sealed class DeliveryCompletedConsumer : EventConsumerBase<DeliveryCompleted>
{
    private readonly ILogger<DeliveryCompletedConsumer> _logger;

    public DeliveryCompletedConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<DeliveryCompletedConsumer> logger)
        : base(connection, serviceProvider, "OrderService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        DeliveryCompleted evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<OrderDbContext>();
        var outboxWriter = scope.GetRequiredService<IOutboxWriter>();

        var order = await db.Orders.FindAsync(new object[] { evt.OrderId }, ct);

        if (order is null)
        {
            _logger.LogWarning("DeliveryCompleted for unknown order {OrderId}", evt.OrderId);
            return;
        }

        if (order.Status != OrderStatus.PickedUp)
        {
            _logger.LogInformation(
                "Skipping DeliveryCompleted for {OrderId} — already in {Status}",
                evt.OrderId, order.Status);
            return;
        }

        order.MarkDelivered(evt.OccurredAt);

        await outboxWriter.AddAsync(new OrderDelivered(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.DeliveredAt!.Value,
            OrderId: order.Id,
            CustomerId: order.CustomerId,
            DeliveredAt: order.DeliveredAt.Value
        ), ct);

        await db.SaveChangesAsync(ct);
    }
}
