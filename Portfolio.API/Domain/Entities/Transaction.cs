using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid WalletId { get; set; }
    public required Wallet Wallet { get; set; }
    public Guid CoinId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double PriceAtTransaction { get; set; }
    public TransactionType TransactionType { get; set; }
}
