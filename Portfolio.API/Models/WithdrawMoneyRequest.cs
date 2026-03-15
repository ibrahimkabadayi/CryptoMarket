namespace Portfolio.API.Models;

public class WithdrawMoneyRequest
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
}
