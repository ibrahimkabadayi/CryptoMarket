namespace Portfolio.API.Domain.Entities;

public class Asset : BaseEntity
{
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal AverageBuyPrice { get; set; }
    public decimal Quantity { get; set; }
}
