using Eaat.Infra.Idempotency;
using Eaat.Infra.Messaging;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eaat.Infra.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEaatInfra<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext, IOutboxDbContext, IProcessedMessagesDbContext
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IProcessedMessagesDbContext>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIdempotencyChecker, IdempotencyChecker>();

        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
