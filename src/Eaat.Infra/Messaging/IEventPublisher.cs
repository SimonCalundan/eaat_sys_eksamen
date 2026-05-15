using Eaat.Contracts;

namespace Eaat.Infra.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : IEvent;
}
