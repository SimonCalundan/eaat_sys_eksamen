using System.Text.Json;
using Eaat.Contracts;
using Eaat.Infra.Idempotency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Eaat.Infra.Messaging;

public abstract class EventConsumerBase<TEvent> : BackgroundService
    where TEvent : IEvent
{
    private readonly RabbitMqConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _serviceName;
    private readonly ILogger _logger;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected EventConsumerBase(
        RabbitMqConnection connection,
        IServiceProvider serviceProvider,
        string serviceName,
        ILogger logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _serviceName = serviceName;
        _logger = logger;
    }

    protected abstract Task HandleAsync(TEvent evt, IServiceProvider scope, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        var eventTypeName = typeof(TEvent).Name;
        var exchange = $"eaat.{eventTypeName}";
        var queue = $"{_serviceName}.{eventTypeName}";

        _channel = await _connection.CreateChannelAsync(stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "{ServiceName} listening for {EventType} on queue {Queue}",
            _serviceName, eventTypeName, queue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected at shutdown
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        var messageId = ea.BasicProperties.MessageId;
        var correlationId = ea.BasicProperties.CorrelationId;
        var eventTypeName = typeof(TEvent).Name;

        try
        {
            var evt = JsonSerializer.Deserialize<TEvent>(ea.Body.Span)
                ?? throw new InvalidOperationException(
                    $"Deserialized {eventTypeName} was null");

            _logger.LogInformation(
                "Received {EventType} {MessageId} (correlation {CorrelationId})",
                eventTypeName, messageId, correlationId);

            using var scope = _serviceProvider.CreateScope();
            var checker = scope.ServiceProvider.GetRequiredService<IIdempotencyChecker>();

            await checker.ExecuteOnceAsync(
                eventId: evt.EventId,
                eventType: eventTypeName,
                handler: ct => HandleAsync(evt, scope.ServiceProvider, ct),
                ct: _stoppingToken);

            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                cancellationToken: _stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed handling {EventType} {MessageId} — requeueing",
                eventTypeName, messageId);

            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: _stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
            _channel = null;
        }
        await base.StopAsync(cancellationToken);
    }
}
