namespace Portfolio.API.Models;

public class DepositMoneyRequestAdress
{
    public string WalletAddress { get; set; } = string.Empty;
    public double Amount { get; set; }
}
