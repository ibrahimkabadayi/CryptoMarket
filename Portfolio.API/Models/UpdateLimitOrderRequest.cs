namespace Portfolio.API.Models;

public class UpdateLimitOrderRequest
{
    public Guid Id { get; set; }
    public decimal? Amount { get; set; }
    public decimal? TargetPrice { get; set; }
}
