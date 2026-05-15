using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Outbox;
using Eaat.OrderService.Domain;
using Eaat.OrderService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eaat.OrderService.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapPost("", PlaceOrder);
        group.MapGet("{id:guid}", GetOrder);

        return app;
    }

    private static async Task<IResult> PlaceOrder(
        PlaceOrderRequest request,
        OrderDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        if (request.CustomerId == Guid.Empty)
            return TypedResults.BadRequest(new { error = "CustomerId is required" });
        if (request.RestaurantId == Guid.Empty)
            return TypedResults.BadRequest(new { error = "RestaurantId is required" });
        if (string.IsNullOrWhiteSpace(request.DeliveryArea))
            return TypedResults.BadRequest(new { error = "DeliveryArea is required" });

        var order = Order.Place(request.CustomerId, request.RestaurantId, request.DeliveryArea);

        db.Orders.Add(order);

        await outboxWriter.AddAsync(new OrderPlaced(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.PlacedAt,
            OrderId: order.Id,
            CustomerId: order.CustomerId,
            RestaurantId: order.RestaurantId,
            DeliveryArea: order.DeliveryArea
        ), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", OrderResponse.From(order));
    }

    private static async Task<IResult> GetOrder(
        Guid id,
        OrderDbContext db,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync(new object[] { id }, ct);

        return order is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(OrderResponse.From(order));
    }
}

public record PlaceOrderRequest(
    Guid CustomerId,
    Guid RestaurantId,
    string DeliveryArea);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    string DeliveryArea,
    string Status,
    DateTimeOffset PlacedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    string? RejectionReason,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? DeliveredAt)
{
    public static OrderResponse From(Order o) => new(
        Id: o.Id,
        CustomerId: o.CustomerId,
        RestaurantId: o.RestaurantId,
        DeliveryArea: o.DeliveryArea,
        Status: o.Status.ToString(),
        PlacedAt: o.PlacedAt,
        AcceptedAt: o.AcceptedAt,
        RejectedAt: o.RejectedAt,
        RejectionReason: o.RejectionReason,
        ReadyAt: o.ReadyAt,
        PickedUpAt: o.PickedUpAt,
        DeliveredAt: o.DeliveredAt);
}
