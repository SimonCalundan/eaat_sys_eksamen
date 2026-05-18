using Eaat.Infra.DependencyInjection;
using Eaat.Infra.Idempotency;
using Eaat.Infra.Outbox;
using Eaat.RestaurantService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eaat.RestaurantService.Persistence;

public class RestaurantDbContext : DbContext, IOutboxDbContext, IProcessedMessagesDbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options) { }

    public DbSet<RestaurantOrder> Orders => Set<RestaurantOrder>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RestaurantOrderConfiguration());
        modelBuilder.ApplyEaatInfraConfigurations();
    }
}
