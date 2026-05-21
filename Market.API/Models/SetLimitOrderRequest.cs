namespace Market.API.Models;

public class SetLimitOrderRequest
{
    public Guid UserId { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal Amount { get; set; }
    public string OrderType { get; set; } = "Buy";
}

