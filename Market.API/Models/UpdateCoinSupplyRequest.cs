namespace Market.API.Models;

public class UpdateCoinSupplyRequest
{
    public decimal? Supply { get; set; }
    public bool? IsCapped { get; set; }
}
