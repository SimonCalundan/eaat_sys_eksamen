using Eaat.Infra.DependencyInjection;
using Eaat.RestaurantService.Consumers;
using Eaat.RestaurantService.Endpoints;
using Eaat.RestaurantService.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RestaurantDb")
    ?? throw new InvalidOperationException("Missing RestaurantDb connection string");

builder.Services.AddDbContext<RestaurantDbContext>(opt =>
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddEaatInfra<RestaurantDbContext>(builder.Configuration);

builder.Services.AddHostedService<OrderPlacedConsumer>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOrdersEndpoints();

app.Run();
