using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messages;

public record BuyCoinEvent
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal BuyAmount { get; set; }
}
