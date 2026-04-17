namespace Portfolio.API.Domain.Entities;

public class TreasuryBalance : BaseEntity
{
    public string AssetSymbol { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    protected TreasuryBalance() { }

    public TreasuryBalance(string assetSymbol)
    {
        AssetSymbol = assetSymbol.ToUpperInvariant();
        TotalAmount = 0;
    }

    public void AddFunds(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Added amount must be bigger than 0.");
        TotalAmount += amount;
    }
}
