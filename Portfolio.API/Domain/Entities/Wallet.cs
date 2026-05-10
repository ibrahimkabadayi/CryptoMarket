namespace Portfolio.API.Domain.Entities;

public class Wallet : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public List<Asset> Assets { get; private set; } = [];
    public List<Transaction> Transactions { get; private set; } = [];
    public decimal FiatBalance { get; private set; } = 0;
    public decimal Value { get; private set; } = 0;

    private Wallet()
    {
        
    }

    public Wallet(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentNullException(nameof(userId), "UserId cannot be empty.");

        Address = "0x" + Guid.NewGuid().ToString("N");
        UserId = userId;
    }

    public void Deposit(decimal fiatAmount)
    {
        FiatBalance += fiatAmount;
        Value += fiatAmount;

        UpdatedDate = DateTime.UtcNow;
    }

    public void Withdraw(decimal fiatAmount)
    {
        if(FiatBalance < fiatAmount)
        {
            throw new ArgumentException("FiatBalance must be bigger than withdraw amount.", nameof(fiatAmount));
        }

        FiatBalance -= fiatAmount;
        Value -= fiatAmount;

        UpdatedDate = DateTime.UtcNow;
    }

    public void DeductValue(decimal amount, decimal costBasis)
    {
        var totalValue = amount * costBasis;
        if (Value < totalValue)
            throw new InvalidOperationException("Not enough wallet value.");

        Value -= totalValue;
        UpdatedDate = DateTime.UtcNow;
    }

    public void AddValue(decimal amount, decimal costBasis)
    {
        Value += amount * costBasis;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Sell(decimal amount, decimal price, decimal feeAmount)
    {
        var totalCost = amount * price;
        if (totalCost < feeAmount)
            throw new InvalidOperationException("Fee amount cannot exceed total cost.");

        FiatBalance += totalCost - feeAmount;
        Value -= amount * price;
        UpdatedDate = DateTime.UtcNow;
    }

    public void RemoveAsset(Asset asset)
    {
        if (!Assets.Contains(asset))
            throw new InvalidOperationException("Asset not found in wallet.");

        Assets.Remove(asset);
    }

    public void Buy(decimal totalCost, decimal feeInCrypto, decimal finalAmount, decimal currentPrice, string symbol, Guid walletId)
    {
        if (FiatBalance < totalCost)
            throw new InvalidOperationException("Insufficient fiat balance.");

        FiatBalance -= totalCost;
        Value += finalAmount * currentPrice;
        UpdatedDate = DateTime.UtcNow;

        var asset = Assets.FirstOrDefault(x => x.Symbol == symbol);
        if (asset != null)
        {
            asset.Receive(finalAmount, currentPrice);
        }
        else
        {
            var newAsset = new Asset(walletId, symbol, currentPrice, finalAmount);
            Assets.Add(newAsset);
        }
    }
}
