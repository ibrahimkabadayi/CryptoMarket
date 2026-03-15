using Notifications.API.Application.DTOs;

namespace Portfolio.API.Application.DTOs;

public class AssetDto : BaseDto
{
    public WalletDto Wallet { get; set; } = new WalletDto();
    public string Symbol { get; set; } = string.Empty;
    public decimal AverageBuyPrice { get; set; }
    public decimal Quantity { get; set; }
}
