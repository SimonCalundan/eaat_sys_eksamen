using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eaat.Infra.Idempotency;

public sealed class IdempotencyChecker : IIdempotencyChecker
{
    private readonly IProcessedMessagesDbContext _context;
    private readonly ILogger<IdempotencyChecker> _logger;

    public IdempotencyChecker(
        IProcessedMessagesDbContext context,
        ILogger<IdempotencyChecker> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ExecuteOnceAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        var alreadyProcessed = await _context.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(p => p.EventId == eventId, ct);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Skipping duplicate {EventType} {EventId}", eventType, eventId);
            return false;
        }

        await handler(ct);

        try
        {
            _context.ProcessedMessages.Add(new ProcessedMessage
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = DateTimeOffset.UtcNow,
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex,
                "Race condition marking {EventType} {EventId} as processed — another consumer reached it first",
                eventType, eventId);
        }

        return true;
    }
}
