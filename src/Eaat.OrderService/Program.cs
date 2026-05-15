using Eaat.Infra.DependencyInjection;
using Eaat.OrderService.Consumers;
using Eaat.OrderService.Endpoints;
using Eaat.OrderService.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Missing OrderDb connection string");

builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddEaatInfra<OrderDbContext>(builder.Configuration);

builder.Services.AddHostedService<OrderAcceptedConsumer>();
builder.Services.AddHostedService<OrderRejectedConsumer>();
builder.Services.AddHostedService<OrderReadyForPickupConsumer>();
builder.Services.AddHostedService<DeliveryAssignedConsumer>();
builder.Services.AddHostedService<DeliveryCompletedConsumer>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOrdersEndpoints();

app.Run();
