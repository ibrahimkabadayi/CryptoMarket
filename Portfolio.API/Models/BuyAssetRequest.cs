namespace Portfolio.API.Models;

public class BuyAssetRequest
{
    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double BuyingPrice { get; set; }
    public double Amount { get; set; }
}
