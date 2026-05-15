using Microsoft.EntityFrameworkCore;

namespace Eaat.Infra.Idempotency;

public interface IProcessedMessagesDbContext
{
    DbSet<ProcessedMessage> ProcessedMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
