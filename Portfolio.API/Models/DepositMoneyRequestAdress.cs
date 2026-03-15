namespace Portfolio.API.Models;

public class DepositMoneyRequestAdress
{
    public string WalletAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
