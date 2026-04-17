namespace Portfolio.API.Application.DTOs;

public class PortfolioDashboardDto
{
    public Guid WalletId { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal FiatBalance { get; set; }
    public decimal TotalInvestedValue { get; set; }
    public List<AssetDashboardDto> Assets { get; set; } = [];
    public List<TransactionDto> RecentTransactions { get; set; } = [];
}
