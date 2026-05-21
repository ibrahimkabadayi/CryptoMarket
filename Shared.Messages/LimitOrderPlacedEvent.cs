namespace Shared.Messages;

public record LimitOrderPlacedEvent
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public decimal TargetPrice { get; init; }
    public decimal Amount { get; init; }
    public string OrderType { get; init; } = "Buy"; 
}
