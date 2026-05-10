namespace Portfolio.API.Domain.Entities;

public class Asset : BaseEntity
{
    public Guid WalletId { get; private set; }
    public Wallet Wallet { get; private set; } = null!;
    public string Symbol { get; private set; } = string.Empty;
    public decimal CostBasis { get; private set; }
    public decimal Quantity { get; private set; }

    private Asset()
    {

    }

    public Asset(Guid walletId, string symbol, decimal costBasis, decimal quantity)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentNullException(nameof(walletId), "Wallet id can not be empty");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be bigger than 0", nameof(quantity));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));
        if (costBasis <= 0)
            throw new ArgumentException("Cost basis cannot be less or equal to 0", nameof(costBasis));

        WalletId = walletId;
        Symbol = symbol;
        CostBasis = costBasis;
        Quantity = quantity;
    }

    public void Deduct(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        if (amount > Quantity)
            throw new InvalidOperationException("Not enough asset amount.");

        Quantity -= amount;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Receive(decimal amount, decimal costBasis)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        CostBasis = ((Quantity * CostBasis) + (amount * costBasis)) / (Quantity + amount);
        Quantity += amount;
        UpdatedDate = DateTime.UtcNow;
    }
}
