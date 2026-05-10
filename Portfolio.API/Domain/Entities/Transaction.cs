using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid WalletId { get; private set; }
    public Wallet Wallet { get; private set; } = null!;
    public Guid AssetId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal PriceAtTransaction { get; private set; }
    public TransactionType TransactionType { get; private set; }

    private Transaction() { }

    public Transaction(Guid walletId, string symbol, decimal amount, TransactionType transactionType, decimal? priceAtTransaction = null)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty.", nameof(walletId));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        WalletId = walletId;
        Symbol = symbol;
        Amount = amount;
        TransactionType = transactionType;
        PriceAtTransaction = priceAtTransaction ?? 0;
    }

    public void AssignAsset(Guid assetId)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId cannot be empty.", nameof(assetId));
        AssetId = assetId;
    }
}