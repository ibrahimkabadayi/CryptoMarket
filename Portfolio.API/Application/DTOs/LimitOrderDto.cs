using Notifications.API.Application.DTOs;
using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class LimitOrderDto : BaseDto
{
    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double LimitPrice { get; set; }
    public double Amount { get; set; }
    public LimitOrderType OrderType { get; set; }
}
