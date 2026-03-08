using Notifications.API.Application.DTOs;

namespace Portfolio.API.Application.DTOs;

public class WalletDto : BaseDto
{
    public string Address { get; set; } = string.Empty;
    public double FiatBalance { get; set; }
    public double Value { get; set; }

    public List<TransactionDto> Transactions { get; set; } = [];
    public List<AssetDto> Assets { get; set; } = [];
}
