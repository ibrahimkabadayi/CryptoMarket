namespace Portfolio.API.Models;

public class CreateLimitOrderRequest
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal Amount { get; set; }
    public int OrderType { get; set; } = 1;
}
