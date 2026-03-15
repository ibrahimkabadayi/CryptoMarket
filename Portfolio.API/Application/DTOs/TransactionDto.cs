using Notifications.API.Application.DTOs;
using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.DTOs;

public class TransactionDto : BaseDto
{
    public WalletDto Wallet { get; set; } = new WalletDto();
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PriceAtTransaction { get; set; }
    public TransactionType TransactionType { get; set; }
}
