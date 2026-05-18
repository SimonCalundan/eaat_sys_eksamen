using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderAcceptedNotificationConsumer : EventConsumerBase<OrderAccepted>
{
    private readonly ILogger<OrderAcceptedNotificationConsumer> _logger;

    public OrderAcceptedNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderAcceptedNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderAccepted evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFY] Order {OrderId} accepted by restaurant {RestaurantId} — preparation started",
            evt.OrderId, evt.RestaurantId);
        return Task.CompletedTask;
    }
}
