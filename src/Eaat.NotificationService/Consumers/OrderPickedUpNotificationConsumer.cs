using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderPickedUpNotificationConsumer : EventConsumerBase<OrderPickedUp>
{
    private readonly ILogger<OrderPickedUpNotificationConsumer> _logger;

    public OrderPickedUpNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderPickedUpNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderPickedUp evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFY {CustomerId}] Your order {OrderId} is on its way — courier {CourierId} picked it up at {PickedUpAt}",
            evt.CustomerId, evt.OrderId, evt.CourierId, evt.PickedUpAt);
        return Task.CompletedTask;
    }
}
