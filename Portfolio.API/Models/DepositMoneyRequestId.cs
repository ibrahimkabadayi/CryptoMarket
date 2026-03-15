namespace Portfolio.API.Models;

public class DepositMoneyRequestId
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
}
