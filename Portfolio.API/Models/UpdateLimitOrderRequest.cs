namespace Portfolio.API.Models;

public class UpdateLimitOrderRequest
{
    public decimal? Amount { get; set; }
    public decimal? TargetPrice { get; set; }
}
