namespace Market.API.Application.DTOs;

public class CoinSupplyDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Supply { get; set; }
    public bool IsCapped { get; set; }
}
