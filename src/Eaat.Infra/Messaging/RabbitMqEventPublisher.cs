using System.Text.Json;
using Eaat.Contracts;
using RabbitMQ.Client;

namespace Eaat.Infra.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqEventPublisher(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<T>(T evt, CancellationToken ct = default)
        where T : IEvent
    {
        var eventType = evt.GetType();
        var exchange = $"eaat.{eventType.Name}";

        await using var channel = await _connection.CreateChannelAsync(ct);

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var body = JsonSerializer.SerializeToUtf8Bytes((object)evt);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = evt.EventId.ToString(),
            CorrelationId = evt.CorrelationId.ToString(),
            Type = eventType.Name,
            Timestamp = new AmqpTimestamp(evt.OccurredAt.ToUnixTimeSeconds()),
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}
