using Eaat.Contracts.Events.Deliveries;
using Eaat.Infra.Messaging;
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
        await db.SaveChangesAsync(ct);
    }
}
