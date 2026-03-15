namespace Market.API.Models;

public class AddCoinRequest
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; } 
    public decimal MarketCap { get; set; }

}
