using Eaat.Infra.DependencyInjection;
using Eaat.Infra.Idempotency;
using Eaat.Infra.Outbox;
using Eaat.OrderService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eaat.OrderService.Persistence;

public class OrderDbContext : DbContext, IOutboxDbContext, IProcessedMessagesDbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyEaatInfraConfigurations();
    }
}
