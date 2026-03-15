using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class LimitOrder : BaseEntity
{
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    public Guid UserId { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal Amount { get; set; }

    public LimitOrderType OrderType { get; set; }
    public LimitOrderStatus OrderStatus { get; set; } = LimitOrderStatus.Pending;

    public DateTime? CompletedAt { get; set; }
}
