using Eaat.Contracts;

namespace Eaat.Infra.Outbox;

public interface IOutboxWriter
{
    Task AddAsync<T>(T evt, CancellationToken ct = default) where T : IEvent;
}
