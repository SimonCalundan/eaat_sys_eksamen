using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

public sealed class OrderReadyForPickupNotificationConsumer : EventConsumerBase<OrderReadyForPickup>
{
    private readonly ILogger<OrderReadyForPickupNotificationConsumer> _logger;

    public OrderReadyForPickupNotificationConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderReadyForPickupNotificationConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        OrderReadyForPickup evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFY] Order {OrderId} is ready for pickup — looking for a courier in {DeliveryArea}",
            evt.OrderId, evt.DeliveryArea);
        return Task.CompletedTask;
    }
}
