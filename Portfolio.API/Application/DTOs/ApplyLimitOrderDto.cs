using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class ApplyLimitOrderDto
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }

    public LimitOrderType OrderType { get; set; }
}
