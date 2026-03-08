namespace Portfolio.API.Models;

public class DepositMoneyRequestId
{
    public Guid WalletId { get; set; }
    public double Amount { get; set; }
}
