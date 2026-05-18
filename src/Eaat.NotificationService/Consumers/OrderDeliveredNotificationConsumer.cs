using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderDeliveredNotificationConsumer : EventConsumerBase<OrderDelivered>
{
    private readonly ILogger<OrderDeliveredNotificationConsumer> _logger;

    public OrderDeliveredNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderDeliveredNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderDelivered evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFY {CustomerId}] Your order {OrderId} has been delivered at {DeliveredAt} — enjoy your meal!",
            evt.CustomerId, evt.OrderId, evt.DeliveredAt);
        return Task.CompletedTask;
    }
}
