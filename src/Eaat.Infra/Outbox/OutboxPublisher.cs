using Eaat.Infra.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Eaat.Infra.Outbox;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqConnection _connection;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        RabbitMqConnection connection,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisher started — polling every {Interval}, batch size {BatchSize}",
            _options.PollInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publishing iteration failed — will retry next poll");
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var pending = await context.Outbox
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.EnqueuedAt)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        _logger.LogInformation("Publishing {Count} pending outbox messages", pending.Count);

        await using var channel = await _connection.CreateChannelAsync(ct);

        foreach (var msg in pending)
        {
            await PublishOneAsync(channel, msg, ct);
            msg.PublishedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }

    private static async Task PublishOneAsync(IChannel channel, OutboxMessage msg, CancellationToken ct)
    {
        var exchange = $"eaat.{msg.EventType}";

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = msg.EventId.ToString(),
            CorrelationId = msg.CorrelationId.ToString(),
            Type = msg.EventType,
            Timestamp = new AmqpTimestamp(msg.OccurredAt.ToUnixTimeSeconds()),
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: msg.Payload,
            cancellationToken: ct);
    }
}
