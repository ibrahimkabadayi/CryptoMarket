using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class LimitOrder : BaseEntity
{
    public Guid WalletId { get; private set; }
    public Wallet Wallet { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal TargetPrice { get; private set; }
    public decimal Amount { get; private set; }
    public LimitOrderType OrderType { get; private set; }
    public LimitOrderStatus OrderStatus { get; private set; } = LimitOrderStatus.Pending;
    public DateTime? CompletedAt { get; private set; }

    private LimitOrder() { }

    public LimitOrder(Guid walletId, Guid userId, string symbol, decimal targetPrice, decimal amount, LimitOrderType orderType)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty.", nameof(walletId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));
        if (targetPrice <= 0)
            throw new ArgumentException("Target price must be positive.", nameof(targetPrice));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        WalletId = walletId;
        UserId = userId;
        Symbol = symbol;
        TargetPrice = targetPrice;
        Amount = amount;
        OrderType = orderType;
    }

    public void Fill()
    {
        OrderStatus = LimitOrderStatus.Filled;
        CompletedAt = DateTime.UtcNow;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Update(decimal? amount, decimal? targetPrice)
    {
        if (amount is <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (targetPrice is <= 0)
            throw new ArgumentException("Target price must be positive.", nameof(targetPrice));

        if (amount.HasValue) Amount = amount.Value;
        if (targetPrice.HasValue) TargetPrice = targetPrice.Value;

        UpdatedDate = DateTime.UtcNow;
    }
}
