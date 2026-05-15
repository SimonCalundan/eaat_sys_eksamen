using Microsoft.EntityFrameworkCore;

namespace Eaat.Infra.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> Outbox { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
