namespace Portfolio.API.Models;

public class BuyAssetRequest
{
    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal BuyingPrice { get; set; }
    public decimal Amount { get; set; }
}
