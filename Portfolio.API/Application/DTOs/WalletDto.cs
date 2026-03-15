using Notifications.API.Application.DTOs;

namespace Portfolio.API.Application.DTOs;

public class WalletDto : BaseDto
{
    public string Address { get; set; } = string.Empty;
    public decimal FiatBalance { get; set; }
    public decimal Value { get; set; }

    public List<TransactionDto> Transactions { get; set; } = [];
    public List<AssetDto> Assets { get; set; } = [];
}
