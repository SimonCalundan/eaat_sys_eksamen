using Eaat.Infra.DependencyInjection;
using Eaat.NotificationService.Consumers;
using Eaat.NotificationService.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("NotificationDb")
    ?? throw new InvalidOperationException("Missing NotificationDb connection string");

builder.Services.AddDbContext<NotificationDbContext>(opt =>
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddEaatInfra<NotificationDbContext>(builder.Configuration);

builder.Services.AddHostedService<OrderPlacedNotificationConsumer>();
builder.Services.AddHostedService<OrderAcceptedNotificationConsumer>();
builder.Services.AddHostedService<OrderRejectedNotificationConsumer>();
builder.Services.AddHostedService<OrderReadyForPickupNotificationConsumer>();
builder.Services.AddHostedService<OrderPickedUpNotificationConsumer>();
builder.Services.AddHostedService<OrderDeliveredNotificationConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
