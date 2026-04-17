namespace Portfolio.API.Application.DTOs;

public class AssetDashboardDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal InvestedAmount => Quantity * AverageBuyPrice; 
}
