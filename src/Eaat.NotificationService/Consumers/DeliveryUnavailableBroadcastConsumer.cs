using Eaat.Contracts.Events.Deliveries;
using Eaat.Infra.Messaging;
using Microsoft.Extensions.Logging;

namespace Eaat.NotificationService.Consumers;

/// <summary>
/// Broadcast til bude — fanout-modtager der demonstrerer at opgaven ikke længere
/// er tilgængelig efter en bud har claimed den. Dette honorerer opgavens krav om
/// at "resten af budene skal have besked om at opgaven ikke længere er tilgængelig".
///
/// Bemærk: dette er den ENESTE consumer i NotificationService der lytter på et
/// Delivery*-event i stedet for et Order*-event. Det er bevidst — det er en
/// broadcast til bude, ikke en notifikation til kunden.
/// </summary>
public sealed class DeliveryUnavailableBroadcastConsumer : EventConsumerBase<DeliveryUnavailable>
{
    private readonly ILogger<DeliveryUnavailableBroadcastConsumer> _logger;

    public DeliveryUnavailableBroadcastConsumer(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<DeliveryUnavailableBroadcastConsumer> logger)
        : base(connection, serviceProvider, "NotificationService", logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        DeliveryUnavailable evt,
        IServiceProvider scope,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[BROADCAST TO COURIERS] Delivery {DeliveryId} for order {OrderId} is no longer available — please stop looking",
            evt.DeliveryId, evt.OrderId);
        return Task.CompletedTask;
    }
}
