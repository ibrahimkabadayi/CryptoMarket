namespace Market.API.Application.DTOs;

public class BuyCoinDto
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double BuyPrice { get; set; }
    public double BuyAmount { get; set; }
}
