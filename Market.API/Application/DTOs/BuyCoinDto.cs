namespace Market.API.Application.DTOs;

public class BuyCoinDto
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal BuyAmount { get; set; }
}
