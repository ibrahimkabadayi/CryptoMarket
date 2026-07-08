namespace Portfolio.API.Domain.Entities;

public class TreasuryBalance : BaseEntity
{
    public string AssetSymbol { get;  set; } = string.Empty;
    public decimal TotalAmount { get; set; } = 0;
}
