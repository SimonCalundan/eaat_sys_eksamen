using Eaat.Contracts.Events.Deliveries;
using Eaat.DeliveryService.Domain;
using Eaat.DeliveryService.Persistence;
using Eaat.Infra.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Eaat.DeliveryService.Endpoints;

public static class DeliveriesEndpoints
{
    public static IEndpointRouteBuilder MapDeliveriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/deliveries");

        group.MapGet("", ListDeliveries);
        group.MapGet("{id:guid}", GetDelivery);
        group.MapPost("{id:guid}/claim", ClaimDelivery);
        group.MapPost("{id:guid}/complete", CompleteDelivery);

        return app;
    }

    private static async Task<IResult> ListDeliveries(
        DeliveryDbContext db,
        CancellationToken ct,
        string? area = null,
        string? status = null)
    {
        var query = db.Deliveries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(area))
            query = query.Where(d => d.DeliveryArea == area);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<DeliveryStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(d => d.Status == parsed);
        }

        var deliveries = await query
            .OrderByDescending(d => d.OfferedAt)
            .Take(100)
            .ToListAsync(ct);

        return TypedResults.Ok(deliveries.Select(DeliveryResponse.From));
    }

    private static async Task<IResult> GetDelivery(
        Guid id,
        DeliveryDbContext db,
        CancellationToken ct)
    {
        var delivery = await db.Deliveries.FindAsync(new object[] { id }, ct);
        return delivery is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(DeliveryResponse.From(delivery));
    }

    private static async Task<IResult> ClaimDelivery(
        Guid id,
        ClaimDeliveryRequest request,
        DeliveryDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        if (request.CourierId == Guid.Empty)
            return TypedResults.BadRequest(new { error = "CourierId is required" });

        var now = DateTimeOffset.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var rowsAffected = await db.Deliveries
            .Where(d => d.Id == id
                     && d.CourierId == null
                     && d.Status == DeliveryStatus.Available)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.CourierId, request.CourierId)
                .SetProperty(d => d.Status, DeliveryStatus.Assigned)
                .SetProperty(d => d.AssignedAt, now), ct);

        if (rowsAffected == 0)
        {
            await tx.RollbackAsync(ct);
            return TypedResults.Conflict(new { error = "Delivery is not available for claiming" });
        }

        var delivery = await db.Deliveries.FindAsync(new object[] { id }, ct);

        await outboxWriter.AddAsync(new DeliveryAssigned(
            EventId: Guid.NewGuid(),
            CorrelationId: delivery!.OrderId,
            OccurredAt: now,
            DeliveryId: delivery.Id,
            OrderId: delivery.OrderId,
            CourierId: request.CourierId
        ), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return TypedResults.Ok(DeliveryResponse.From(delivery));
    }

    private static async Task<IResult> CompleteDelivery(
        Guid id,
        CompleteDeliveryRequest request,
        DeliveryDbContext db,
        IOutboxWriter outboxWriter,
        CancellationToken ct)
    {
        if (request.CourierId == Guid.Empty)
            return TypedResults.BadRequest(new { error = "CourierId is required" });

        var delivery = await db.Deliveries.FindAsync(new object[] { id }, ct);
        if (delivery is null) return TypedResults.NotFound();

        try
        {
            delivery.Complete(request.CourierId, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }

        await outboxWriter.AddAsync(new DeliveryCompleted(
            EventId: Guid.NewGuid(),
            CorrelationId: delivery.OrderId,
            OccurredAt: delivery.CompletedAt!.Value,
            DeliveryId: delivery.Id,
            OrderId: delivery.OrderId,
            CourierId: delivery.CourierId!.Value,
            CompletedAt: delivery.CompletedAt.Value
        ), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(DeliveryResponse.From(delivery));
    }
}

public record ClaimDeliveryRequest(Guid CourierId);

public record CompleteDeliveryRequest(Guid CourierId);

public record DeliveryResponse(
    Guid Id,
    Guid OrderId,
    Guid RestaurantId,
    string PickupAddress,
    string DeliveryArea,
    string Status,
    Guid? CourierId,
    DateTimeOffset OfferedAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? CompletedAt)
{
    public static DeliveryResponse From(Delivery d) => new(
        Id: d.Id,
        OrderId: d.OrderId,
        RestaurantId: d.RestaurantId,
        PickupAddress: d.PickupAddress,
        DeliveryArea: d.DeliveryArea,
        Status: d.Status.ToString(),
        CourierId: d.CourierId,
        OfferedAt: d.OfferedAt,
        AssignedAt: d.AssignedAt,
        CompletedAt: d.CompletedAt);
}
