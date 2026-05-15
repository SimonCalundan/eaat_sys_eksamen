using Eaat.Infra.Idempotency;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Eaat.Infra.DependencyInjection;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyEaatInfraConfigurations(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new ProcessedMessageConfiguration());
        return builder;
    }
}
