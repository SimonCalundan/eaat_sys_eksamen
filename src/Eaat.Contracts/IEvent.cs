namespace Eaat.Contracts;

public interface IEvent
{
    Guid EventId { get; }             
    Guid CorrelationId { get; }        
    DateTimeOffset OccurredAt { get; } 
}