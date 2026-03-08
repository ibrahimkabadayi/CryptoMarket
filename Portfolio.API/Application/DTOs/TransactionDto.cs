using Notifications.API.Application.DTOs;
using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class TransactionDto : BaseDto
{
    public WalletDto Wallet { get; set; } = new WalletDto();
    public string Symbol { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double PriceAtTransaction { get; set; }
    public TransactionType TransactionType { get; set; }
}
