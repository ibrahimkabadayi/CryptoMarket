using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class LimitOrderCacheDto
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal Amount { get; set; }
    public LimitOrderType OrderType { get; set; }
    public LimitOrderStatus OrderStatus { get; set; }
    public DateTime? CompletedAt { get; set; }
}
