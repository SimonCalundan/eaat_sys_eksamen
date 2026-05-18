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

public sealed class DeliveryAssignedConsumer : EventConsumerBase<DeliveryAssigned>
{
    private readonly ILogger<DeliveryAssignedConsumer> _logger;

    public DeliveryAssignedConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<DeliveryAssignedConsumer> logger)
        : base(connection, serviceProvider, "OrderService", logger)
    {
        _logger = logger;
    }

    protected override async Task HandleAsync(
        DeliveryAssigned evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        var db = scope.GetRequiredService<OrderDbContext>();
        var outboxWriter = scope.GetRequiredService<IOutboxWriter>();

        var order = await db.Orders.FindAsync(new object[] { evt.OrderId }, ct);

        if (order is null)
        {
            _logger.LogWarning("DeliveryAssigned for unknown order {OrderId}", evt.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Ready)
        {
            _logger.LogInformation(
                "Skipping DeliveryAssigned for {OrderId} — already in {Status}",
                evt.OrderId, order.Status);
            return;
        }

        order.MarkPickedUp(evt.OccurredAt);

        await outboxWriter.AddAsync(new OrderPickedUp(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.PickedUpAt!.Value,
            OrderId: order.Id,
            CustomerId: order.CustomerId,
            CourierId: evt.CourierId,
            PickedUpAt: order.PickedUpAt.Value
        ), ct);

        await db.SaveChangesAsync(ct);
    }
}
