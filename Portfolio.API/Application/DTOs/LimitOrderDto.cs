using Notifications.API.Application.DTOs;
using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class LimitOrderDto : BaseDto
{
    public Guid WalletId { get; set; }
    public WalletDto Wallet { get; set; } = null!;

    public Guid UserId { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal Amount { get; set; }

    public LimitOrderType OrderType { get; set; }
    public LimitOrderStatus OrderStatus { get; set; }

    public DateTime? CompletedAt { get; set; }

}
