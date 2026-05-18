using Eaat.DeliveryService.Consumers;
using Eaat.DeliveryService.Endpoints;
using Eaat.DeliveryService.Persistence;
using Eaat.Infra.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DeliveryDb")
    ?? throw new InvalidOperationException("Missing DeliveryDb connection string");

builder.Services.AddDbContext<DeliveryDbContext>(opt =>
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddEaatInfra<DeliveryDbContext>(builder.Configuration);

builder.Services.AddHostedService<OrderReadyForPickupConsumer>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDeliveriesEndpoints();

app.Run();
