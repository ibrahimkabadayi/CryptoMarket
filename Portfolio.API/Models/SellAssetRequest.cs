namespace Portfolio.API.Models;

public class SellAssetRequest
{
    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Price { get; set; }
}
