namespace Portfolio.API.Domain.Entities;

public class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    public string Address { get; set; } = string.Empty;
    public List<Asset>? Assets { get; set; }
    public List<Transaction>? Transactions { get; set; }
    public double FiatBalance { get; set; } = 0;
    public double Value { get; set; } = 0;
}
