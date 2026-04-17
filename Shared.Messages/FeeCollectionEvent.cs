namespace Shared.Messages;

public record FeeCollectionEvent
{
    public string Symbol { get; set; } = string.Empty; 
    public decimal FeeAmount { get; set; }
    public Guid UserId { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
