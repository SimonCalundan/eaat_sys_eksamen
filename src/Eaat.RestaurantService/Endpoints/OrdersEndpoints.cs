using Eaat.Contracts.Events.Orders;
using Eaat.Infra.Outbox;
using Eaat.RestaurantService.Domain;
using Eaat.RestaurantService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eaat.RestaurantService.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapGet("", ListOrders);
        group.MapGet("{id:guid}", GetOrder);
        group.MapPost("{id:guid}/accept", AcceptOrder);
        group.MapPost("{id:guid}/reject", RejectOrder);
        group.MapPost("{id:guid}/ready", MarkOrderReady);

        return app;
    }

    private static async Task<IResult> ListOrders(
        RestaurantDbContext db,
        CancellationToken ct,
        string? status = null)
    {
        var query = db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RestaurantOrderStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        var orders = await query
            .OrderByDescending(o => o.ReceivedAt)
            .Take(100)
            .ToListAsync(ct);

        return TypedResults.Ok(orders.Select(RestaurantOrderResponse.From));
    }

    private static async Task<IResult> GetOrder(
        Guid id,
        RestaurantDbContext db,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync(new object[] { id }, ct);
        return order is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(RestaurantOrderResponse.From(order));
    }

    private static async Task<IResult> AcceptOrder(
        Guid id,
        RestaurantDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync(new object[] { id }, ct);
        if (order is null) return TypedResults.NotFound();

        try
        {
            order.Accept(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }

        await outboxWriter.AddAsync(new OrderAccepted(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.AcceptedAt!.Value,
            OrderId: order.Id,
            RestaurantId: order.RestaurantId
        ), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(RestaurantOrderResponse.From(order));
    }

    private static async Task<IResult> RejectOrder(
        Guid id,
        RejectOrderRequest request,
        RestaurantDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return TypedResults.BadRequest(new { error = "Reason is required" });

        var order = await db.Orders.FindAsync(new object[] { id }, ct);
        if (order is null) return TypedResults.NotFound();

        try
        {
            order.Reject(request.Reason, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }

        await outboxWriter.AddAsync(new OrderRejected(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.RejectedAt!.Value,
            OrderId: order.Id,
            RestaurantId: order.RestaurantId,
            Reason: request.Reason
        ), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(RestaurantOrderResponse.From(order));
    }

    private static async Task<IResult> MarkOrderReady(
        Guid id,
        RestaurantDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync(new object[] { id }, ct);
        if (order is null) return TypedResults.NotFound();

        try
        {
            order.MarkReady(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }

        await outboxWriter.AddAsync(new OrderReadyForPickup(
            EventId: Guid.NewGuid(),
            CorrelationId: order.Id,
            OccurredAt: order.ReadyAt!.Value,
            OrderId: order.Id,
            RestaurantId: order.RestaurantId,
            PickupAddress: $"Restaurant {order.RestaurantId}",
            DeliveryArea: order.DeliveryArea
        ), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(RestaurantOrderResponse.From(order));
    }
}

public record RejectOrderRequest(string Reason);

public record RestaurantOrderResponse(
    Guid Id,
    Guid RestaurantId,
    Guid CustomerId,
    string DeliveryArea,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    string? RejectionReason,
    DateTimeOffset? ReadyAt)
{
    public static RestaurantOrderResponse From(RestaurantOrder o) => new(
        Id: o.Id,
        RestaurantId: o.RestaurantId,
        CustomerId: o.CustomerId,
        DeliveryArea: o.DeliveryArea,
        Status: o.Status.ToString(),
        ReceivedAt: o.ReceivedAt,
        AcceptedAt: o.AcceptedAt,
        RejectedAt: o.RejectedAt,
        RejectionReason: o.RejectionReason,
        ReadyAt: o.ReadyAt);
}
