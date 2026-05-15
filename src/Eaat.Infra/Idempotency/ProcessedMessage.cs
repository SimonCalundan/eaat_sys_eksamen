namespace Eaat.Infra.Idempotency;

public class ProcessedMessage
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTimeOffset ProcessedAt { get; set; }
}
