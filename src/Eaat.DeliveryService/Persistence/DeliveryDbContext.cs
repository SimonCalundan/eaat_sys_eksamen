using Eaat.DeliveryService.Domain;
using Eaat.Infra.DependencyInjection;
using Eaat.Infra.Idempotency;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Eaat.DeliveryService.Persistence;

public class DeliveryDbContext : DbContext, IOutboxDbContext, IProcessedMessagesDbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options) { }

    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeliveryConfiguration());
        modelBuilder.ApplyEaatInfraConfigurations();
    }
}
