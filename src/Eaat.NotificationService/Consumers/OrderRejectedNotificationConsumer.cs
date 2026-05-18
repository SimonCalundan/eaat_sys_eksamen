using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderRejectedNotificationConsumer : EventConsumerBase<OrderRejected>
{
    private readonly ILogger<OrderRejectedNotificationConsumer> _logger;

    public OrderRejectedNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderRejectedNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderRejected evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "[NOTIFY] Order {OrderId} was rejected by restaurant {RestaurantId}. Reason: {Reason}",
            evt.OrderId, evt.RestaurantId, evt.Reason);
        return Task.CompletedTask;
    }
}
