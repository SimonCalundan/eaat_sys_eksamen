using Eaat.Infra.DependencyInjection;
using Eaat.Infra.Idempotency;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Eaat.NotificationService.Persistence;

public class NotificationDbContext : DbContext, IOutboxDbContext, IProcessedMessagesDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyEaatInfraConfigurations();
    }
}
