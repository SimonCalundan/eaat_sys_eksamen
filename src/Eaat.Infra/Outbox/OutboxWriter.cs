using System.Text.Json;
using Eaat.Contracts;

namespace Eaat.Infra.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly IOutboxDbContext _context;

    public OutboxWriter(IOutboxDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync<T>(T evt, CancellationToken ct = default) where T : IEvent
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes((object)evt);

        var message = new OutboxMessage
        {
            EventId = evt.EventId,
            CorrelationId = evt.CorrelationId,
            OccurredAt = evt.OccurredAt,
            EventType = evt.GetType().Name,
            Payload = payload,
            EnqueuedAt = DateTimeOffset.UtcNow,
            PublishedAt = null,
        };

        await _context.Outbox.AddAsync(message, ct);
    }
}
