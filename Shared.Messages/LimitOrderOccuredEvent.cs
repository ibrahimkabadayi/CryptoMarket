namespace Shared.Messages;

public record LimitOrderOccuredEvent
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Ordertype { get; set; } = "Buy";
}
