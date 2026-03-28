using System.Linq.Expressions;

namespace Shared.Messages;

public record CoinPriceEvent
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
