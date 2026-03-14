using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Entities;

public class LimitOrder : BaseEntity
{
    public Guid WalletId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double LimitPrice { get; set; }
    public double Amount { get; set; }
    public LimitOrderType OrderType { get; set; }
}
