using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderPlacedNotificationConsumer : EventConsumerBase<OrderPlaced>
{
    private readonly ILogger<OrderPlacedNotificationConsumer> _logger;

    public OrderPlacedNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderPlacedNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderPlaced evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFY {CustomerId}] Your order {OrderId} has been received and sent to restaurant {RestaurantId}",
            evt.CustomerId, evt.OrderId, evt.RestaurantId);
        return Task.CompletedTask;
    }
}
