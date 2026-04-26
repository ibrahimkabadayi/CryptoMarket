using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = new Wallet();
    public Guid AssetId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PriceAtTransaction { get; set; }
    public TransactionType TransactionType { get; set; }
}
