namespace Eaat.Infra.Outbox;

public class OutboxMessage
{
    public Guid EventId { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string EventType { get; set; } = default!;
    public byte[] Payload { get; set; } = default!;
    public DateTimeOffset EnqueuedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
