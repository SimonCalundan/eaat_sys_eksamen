namespace Eaat.Infra.Idempotency;

public interface IIdempotencyChecker
{
    Task<bool> ExecuteOnceAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handler,
        CancellationToken ct = default);
}
